using DG.Tweening;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using static Define;
using static UnityEngine.Rendering.DebugUI;

public class MeteorController : BaseController, IDamageable, IStatusTarget
{
    public MeteorStat Stat { get; private set; }
    public MeteorMovement Movement { get; private set; }
    public StatusEffectReceiver Status { get; private set; }
    public MeteorVisual Visual { get; private set; }

    [SerializeField]
    private float _currentHp;

    [SerializeField]
    private float _maxHp;
    UI_HpBar _myHpBar;

    [Header("Debug Gizmo")]
    [SerializeField] private bool _showGizmo = false;     // 기즈모를 켜고 끌 수 있는 스위치
    [SerializeField] private float _gizmoRadius = 3f;    // 씬 창에서 눈으로 볼 테스트용 반경
    [SerializeField] private Color _gizmoColor = Color.green; // 기즈모 선 색상

    [Header("밀어내기 및 데미지 설정")]
    public float pushForce = 5f;
    public float damageCooldown = 0.5f; // 메테오가 플레이어를 지질 때의 데미지 간격
    private float _currentDamageTimer = 0f;

    // MeteorController.cs 내부
    public Coroutine ActionCoroutine; // 행동(패턴) 코루틴을 저장할 변수

    private static readonly Collider2D[] _shatterColliders = new Collider2D[20];
    private static ContactFilter2D _shatterFilter;
    private static bool _isFilterInitialized = false;

    private void Awake()
    {
        // 같은 게임오브젝트에 붙어있는 모듈들을 찾아옵니다.
        Movement = Util.GetOrAddComponent<MeteorMovement>(gameObject);
        Status = Util.GetOrAddComponent<StatusEffectReceiver>(gameObject); // 새로 만든 공용 리시버 부착
        Visual = Util.GetOrAddComponent<MeteorVisual>(gameObject);

        // 2. 필터 초기화 (모든 몬스터가 똑같은 필터를 쓰므로 한 번만 세팅하면 됩니다)
        if (!_isFilterInitialized)
        {
            _shatterFilter = new ContactFilter2D();
            _shatterFilter.useLayerMask = true;
            // FireBullet이나 IceBullet에서 타겟팅할 레이어와 똑같이 맞춰주세요
            _shatterFilter.layerMask = LayerMask.GetMask("Meteor", "Boss");
            _isFilterInitialized = true;
        }
    }

    public void Init(Vector2 pos, MeteorStat stat)
    {
        if (stat == null) return;

        Stat = stat;
        _maxHp = Managers.Stage.GetCalculatedMeteorHp(Stat.MaxHp.TotalValue);
        _currentHp = _maxHp;

        // 1.위치 설정
        Movement.Init(pos, Stat);
        Status.Init();
        Visual.Init();

        Stat.Behavior?.OnInit(this);

        GameObject hpBarGo = Managers.Resource.Instantiate("UI/World/UI_HpBar");
        if (hpBarGo != null)
        {
            _myHpBar = hpBarGo.GetComponent<UI_HpBar>();
            _myHpBar.SetTarget(this.transform); // 나를 따라다니라고 설정
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.OnDamage(Stat.Damage.TotalValue, false, this.gameObject);
                _currentDamageTimer = 0f; // 데미지를 줬으니 타이머 초기화
            }
        }
    }

    public void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.attachedRigidbody;
            if (playerRb != null)
            {
                Vector2 pushDirection = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(pushDirection * pushForce, ForceMode2D.Force);
            }

            _currentDamageTimer += Time.fixedDeltaTime;
            if (_currentDamageTimer >= damageCooldown)
            {
                _currentDamageTimer = 0f;
                PlayerController player = collision.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    player.OnDamage(Stat.Damage.TotalValue, false, this.gameObject);
                }
            }
        }
    }

    private void OnEnable()
    {
        Managers.Game.AddActiveObject(this);
    }

    private void OnDisable()
    {
        if (Stat != null)
        {
            Stat.Behavior?.OnRelease(this);
        }
        if (_myHpBar != null)
        {
            Managers.Resource.Destroy(_myHpBar.gameObject);
            _myHpBar = null;
        }
        Managers.Game.RemoveActiveObject(this);
    }

    protected override void OnUpdate()
    {
        Stat.Behavior?.OnUpdate(this);
    }

    public void OnDamage(float damage, bool isCritical = false, GameObject attacker = null)
    {
        if (!gameObject.activeInHierarchy || _currentHp <= 0) return;

        if (damage > 0)
        {
            if (Status.HasAuraBuff)
            {
                damage *= 0.5f;
            }

            if (Status.HasShockDebuff)
            {
                damage *= 1.5f;
                Status.ShockDebuffOff();
                Managers.Effect.Play(EffectType.Meteor_ShockHit, transform.position);
                Managers.Sound.Play(Define.SoundID.Sfx_Lightning_Hit);
            }
            else
            {
                Managers.Sound.Play(Define.SoundID.Sfx_NormalBulletHit);
            }

            _currentHp -= damage;
            ShowDamageText(damage, isCritical);
            Visual.PlayHitFlash();

            if (_currentHp <= 0)
            {
                Die();
            }
            else
            {
                if (_myHpBar != null)
                {
                    _myHpBar.UpdateHP(_currentHp, _maxHp);
                }
            }
        }
    }

    private void ShowDamageText(float damage, bool isCritical = false)
    {
        Vector3 textPos = transform.position + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0.5f, 0);
        GameObject go = Managers.Resource.Instantiate("DamageText");
        DamageText damageText = go.GetOrAddComponent<DamageText>();
        if (damageText != null)
        {
            damageText.Init(textPos, Mathf.FloorToInt(damage), isCritical);
        }
    }

    private void Die()
    {
        if (Status.hasIcePuddleMark)
        {
            GameObject puddleGo = Managers.Effect.Play(Define.EffectType.IceBullet_Explosion, transform.position);

            if (puddleGo != null)
            {
                puddleGo.transform.position = transform.position;

                if (puddleGo.TryGetComponent(out IceZoneController puddle))
                {
                    puddle.Init(Status.icePuddleDamage, Status.icePuddleRadius, Status.icePuddleSlowPercent);
                }
            }
        }

        Visual.ReturnColor();
        Stat.Behavior?.OnDie(this);
        Managers.Level.AddScore(Mathf.FloorToInt(Stat.Score.TotalValue));

        DropItem();
        Managers.Game.AddKillCount();
        Managers.Resource.Destroy(gameObject);
    }

    private void DropItem()
    {
        bool isFirstItem = true;
        float scatterRadius = 0.5f;

        foreach (var drop in Stat.dropTable)
        {
            float roll = UnityEngine.Random.value;
            if (roll <= drop.dropRate)
            {
                int customVal = 0;
                if (drop.itemType == Define.ItemType.Exp)
                {
                    customVal = Mathf.FloorToInt(Stat.Exp.TotalValue);
                }

                Vector3 spawnPos = transform.position;

                if (!isFirstItem)
                {
                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * scatterRadius;
                    spawnPos += (Vector3)randomOffset;
                }

                Managers.Game.spawner.SpawnDropItem(spawnPos, drop.itemType, customVal);
                isFirstItem = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (!_showGizmo) return;

        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, _gizmoRadius);

        if (Stat != null && Stat.type == MeteorType.AuraBuffMeteor)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Stat.auraRadius.TotalValue);
        }
    }

    // ==========================================================
    // IStatusTarget 인터페이스 구현부 (StatusEffectReceiver에서 호출됨)
    // ==========================================================

    public void TakeStatusDamage(float damage)
    {
        // 상태 이상(화상 등)으로 인한 틱 데미지 적용
        // 크리티컬 효과나 가해자 표시는 없으므로 기본값(false, null)으로 넘깁니다.
        OnDamage(damage);
    }

    public void AddSpeedMultiplier(float multiplier)
    {
        // 이동 컴포넌트의 배율을 추가하고 속도를 재계산
        Movement.currentSpeed.AddMultiplier(multiplier);
        Movement.UpdateVelocity();
    }

    public void SubSpeedMultiplier(float multiplier)
    {
        // 이동 컴포넌트의 배율을 빼고 속도를 재계산
        Movement.currentSpeed.SubMultiplier(multiplier);
        Movement.UpdateVelocity();
    }

    public void SetForceZeroSpeed(bool isZero)
    {
        // 빙결 시 강제로 속도를 0으로 고정하거나 해제
        Movement.currentSpeed.SetForceZero(isZero);
        Movement.UpdateVelocity();
    }

    public void SetStatusColor(Color color)
    {
        // 비주얼 컴포넌트의 색상 변경 기능 호출
        Visual.SetColor(color);
    }

    public void ResetStatusColor()
    {
        // 비주얼 컴포넌트를 원래 색상으로 복구
        Visual.SetColor(Color.white);
    }
    // MeteorController.cs 내부
    private void OnDrawGizmosSelected()
    {
        // 현재 행동이 ExplosionMeteorBehavior 타입일 때만 그립니다.
        if (Stat.Behavior is ExplosionMeteorBehavior)
        {
            Gizmos.color = Color.red;
            // 폭발 반경(explosionRadius)을 기즈모로 표시
            Gizmos.DrawWireSphere(transform.position, Stat.explosionRadius.TotalValue);

            // 추가로 감지 반경도 확인하고 싶다면 아래 주석 해제
            // Gizmos.color = Color.yellow;
            // Gizmos.DrawWireSphere(transform.position, Stat.explosionTargetRadius.TotalValue);
        }
    }
}