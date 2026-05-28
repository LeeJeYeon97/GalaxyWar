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

public class MeteorController : BaseController, IDamageable
{

    public MeteorStat Stat { get; private set; }
    public MeteorMovement Movement { get; private set; }
    public MeteorStatus Status { get; private set; }
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

    private void Awake()
    {
        // 같은 게임오브젝트에 붙어있는 모듈들을 찾아옵니다.
        Movement = Util.GetOrAddComponent<MeteorMovement>(gameObject);
        Status = Util.GetOrAddComponent<MeteorStatus>(gameObject);
        Visual = Util.GetOrAddComponent<MeteorVisual>(gameObject);
    }
    public void Init(Vector2 pos, MeteorStat stat)
    {
        if (stat == null)
        {
            return;
        }
        Stat = stat;
        _maxHp = Managers.Stage.GetCalculatedMeteorHp(Stat.MaxHp.TotalValue);
        _currentHp = _maxHp;
        // 1.위치 설정

        Movement.Init(pos,Stat);
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
    //  닿는 순간 즉시 1번 데미지를 줍니다.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            //  TryGetComponent 대신 GetComponentInParent를 사용하고 null인지 체크합니다.
            PlayerController player = collision.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                player.OnDamage(Stat.Damage.TotalValue);
                _currentDamageTimer = 0f; // 데미지를 줬으니 타이머 초기화
            }
        }
    }
    // 닿아있는 동안에는 '계속 밀어내기' + '주기적으로 데미지 주기'를 수행합니다.
    public void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 1. 밀어내기는 매 프레임 부드럽게 실행
            Rigidbody2D playerRb = collision.attachedRigidbody;
            if (playerRb != null)
            {
                Vector2 pushDirection = (collision.transform.position - transform.position).normalized;
                playerRb.AddForce(pushDirection * pushForce, ForceMode2D.Force);
            }

            // 2. 데미지는 Time.fixedDeltaTime을 더해가며 쿨타임이 찼을 때만 실행
            _currentDamageTimer += Time.fixedDeltaTime;
            if (_currentDamageTimer >= damageCooldown)
            {
                _currentDamageTimer = 0f; // 타이머 리셋

                //  여기서도 부모까지 탐색하도록 변경합니다.
                PlayerController player = collision.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    player.OnDamage(Stat.Damage.TotalValue);
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
        // 3. 내가 죽으면 빌려 쓴 HP바도 풀에 반납
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

    public void OnDamage(float damage, bool isCritical = false)
    {
        if (!gameObject.activeInHierarchy || _currentHp <= 0) return;

        if (damage > 0)
        {
            // 핵심: Stat 원본을 수정하지 않고, 들어온 damage 변수값만 즉석에서 반토막 냅니다!
            if (Status.HasAuraBuff)
            {
                damage *= 0.5f; // 오라를 받고 있다면 데미지 50% 감소
            }
            // [추가된 부분] 감전(Shock) 디버프가 있다면 터트리고 데미지 증폭!
            if (Status.ConsumeShock())
            {
                damage *= 1.5f;

                // 파티클 터트리기
                Managers.Effect.Play(EffectType.Meteor_ShockHit, transform.position);
                Managers.Sound.Play(Define.SoundID.Sfx_Lightning_Hit);
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
            damageText.Init(textPos, Mathf.FloorToInt(damage),isCritical);
        }
    }
    private void Die()
    {
        Visual.ReturnColor();
        Stat.Behavior?.OnDie(this);
        Managers.Level.AddScore(Mathf.FloorToInt(Stat.Score.TotalValue));
        
        DropItem();
        Managers.Game.AddKillCount();
        Managers.Resource.Destroy(gameObject);
    }
    private void DropItem()
    {
        foreach(var drop in Stat.dropTable)
        {
            // 0.0 ~ 1.0 사이의 랜덤 값을 뽑습니다.
            float roll = UnityEngine.Random.value;

            // 주사위 값이 설정된 확률보다 낮거나 같다면 당첨!
            if (roll <= drop.dropRate)
            {
                int customVal = 0;

                // 생성할 아이템이 경험치(Exp) 구슬이라면 메테오의 경험치를 담아줍니다!
                if (drop.itemType == Define.ItemType.Exp)
                {
                    customVal = Mathf.FloorToInt(Stat.Exp.TotalValue);
                }
                // 스포너에게 생성을 요청합니다.
                Managers.Game.spawner.SpawnDropItem(transform.position, drop.itemType, customVal);
            }
        }
    }

    // 오브젝트가 선택되었을 때만 그리고 싶다면 OnDrawGizmos 대신 OnDrawGizmosSelected를 사용하셔도 좋습니다.
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

            // 1. 인스펙터에서 스위치를 껐다면 그리지 않음
        if (!_showGizmo) return;

        // 2. 에디터(인스펙터)에서 설정한 테스트용 기즈모 그리기 (게임 플레이 전에도 보임!)
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, _gizmoRadius);

        // 3. 기존의 오라 운석 런타임 기즈모 (게임 실행 중에만 덮어씌워서 그림)
        if (Stat != null && Stat.type == MeteorType.AuraBuffMeteor)
        {
            // 런타임에는 진짜 스탯 값(auraRadius)을 노란색으로 그립니다.
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Stat.auraRadius.TotalValue);
        }
    }
}
