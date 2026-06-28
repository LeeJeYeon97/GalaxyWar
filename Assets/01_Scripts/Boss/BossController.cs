using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using static Define;

public class BossController : BaseController, IDamageable, IStatusTarget
{
    public Rigidbody2D Rb;
    public Collider2D Collider;

    public float currentHp;
    public Transform firePoint;         // 총알이 나가는 위치 (보스 입이나 하단)

    public bool _isDead = false;
    private bool _isAttacking = false;

    public BossStat Stat;
    public GameObject attackTarget;

    UI_HpBar _myHpBar;

    public MeshRenderer _meshRenderer;
    private Coroutine _flashCoroutine;

    // [추가] 공용 상태 이상 리시버
    public StatusEffectReceiver Status { get; private set; }

    // [추가] 보스 전용 속도 제어 변수 (메테오의 Movement.currentSpeed 역할을 대신함)
    private float _speedMultiplier = 1f;
    private bool _isForceZeroSpeed = false;

    //  [추가] 체력이 변할 때마다 발동할 이벤트 (현재 체력, 최대 체력)
    public event Action<float, float> OnHpChanged;

    //  [추가] 일시 정지 상태를 백업하기 위한 변수들
    private bool _isPaused = false;
    private Vector2 _savedVelocity;
    private float _savedAngularVelocity;

    private SpriteRenderer _spriteRenderer;

    //  MPB를 캐싱해두고 재사용하기 위한 변수
    private MaterialPropertyBlock _mpb;

    // Shader.PropertyToID를 쓰면 문자열 연산을 매번 하지 않아 성능이 더 좋습니다.
    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");

    private void Awake()
    {
        // 상태 이상 매니저 부착 및 캐싱
        Status = Util.GetOrAddComponent<StatusEffectReceiver>(gameObject);
        _spriteRenderer = GetComponent<SpriteRenderer>();
        // Awake에서 딱 한 번만 할당합니다.
        _mpb = new MaterialPropertyBlock();
    }

    public void Init(BossStat stat, Vector2 startPos, GameObject target)
    {
        
        // 타겟을 계속 따라가도록 설정
        Stat = stat;
        currentHp = Stat.MaxHp.TotalValue;
        transform.localPosition = startPos;

        // 타겟 방향으로 방향 지정
        attackTarget = target;

        _isDead = false;

        // [초기화] 상태이상 및 속도 관련 변수 초기화
        Status.Init();
        _speedMultiplier = 1f;
        _isForceZeroSpeed = false;

        //  [추가] 압도적인 상단 보스 체력바 팝업 띄우기!
        UI_BossHpBarPopup bossHpBar = Managers.UI.ShowPopupUI<UI_BossHpBarPopup>();

        // 띄운 팝업에게 "내(this) 체력을 보고 업데이트해!" 라고 연결해줍니다.
        if (bossHpBar != null)
        {
            bossHpBar.SetTargetBoss(this, "TEST"); // 이름은 자유롭게!
        }

        StartCoroutine(BossPatternLoop());
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        // =========================================================
        //  1. 게임 일시 정지(Pause) 시 물리 상태 저장 및 잠금
        // =========================================================
        if (Managers.Game.currentGameState == GameState.Pause)
        {
            if (!_isPaused)
            {
                _isPaused = true;
                if (Rb != null)
                {
                    _savedVelocity = Rb.linearVelocity;
                    _savedAngularVelocity = Rb.angularVelocity;

                    Rb.linearVelocity = Vector2.zero;
                    Rb.angularVelocity = 0f;
                    Rb.bodyType = RigidbodyType2D.Kinematic;
                }
            }
            return;
        }
        // =========================================================
        //  2. 게임 재개(Play) 시 물리 상태 복구
        // =========================================================
        else
        {
            if (_isPaused)
            {
                _isPaused = false;
                if (Rb != null)
                {
                    Rb.bodyType = RigidbodyType2D.Dynamic;
                    Rb.linearVelocity = _savedVelocity;
                    Rb.angularVelocity = _savedAngularVelocity;
                }
            }
        }

        // 1. 타겟이 없거나 보스가 죽었으면 추적 중지
        if (_isDead || attackTarget == null) return;

        // 2. 보스의 이동 속도 가져오기 
        // [변경점] 상태 이상(슬로우/빙결)에 의해 변형된 속도를 적용합니다!
        float moveSpeed = Stat.Speed.TotalValue * _speedMultiplier;
        if (_isForceZeroSpeed) moveSpeed = 0f; // 얼어붙었으면 속도 0

        // 3. 타겟을 향해 '일정한 속도(moveSpeed)'로 계속 이동
        transform.position = Vector2.MoveTowards(
            transform.position,
            attackTarget.transform.position,
            moveSpeed * Time.deltaTime
        );

        //  4. 타겟을 바라보도록 부드럽게 회전하기 
        Vector2 direction = attackTarget.transform.position - transform.position;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float offsetAngle = -90f; // ⬅️ 보스 이미지가 엉뚱한 곳을 본다면 이 숫자를 수정!
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle + offsetAngle);

        // 즉시 휙! 돌지 않고, 보스 특유의 묵직한 느낌으로 부드럽게 회전시킵니다.
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
    }

    private IEnumerator BossPatternLoop()
    {
        yield return new WaitForSeconds(2f); // 등장 대기 시간

        while (!_isDead)
        {
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                yield return new WaitUntil(() => Managers.Game.currentGameState == GameState.Playing);
            }

            // 패턴 리스트가 비어있지 않고, 공격 중이 아닐 때
            // [추가] 얼어붙어 있을 때는 패턴을 사용하지 않도록 막습니다.
            if (!_isAttacking && Stat.myPatterns.Count > 0 && !_isForceZeroSpeed)
            {
                _isAttacking = true;

                BossPatternSO selectedPattern = Stat.myPatterns[UnityEngine.Random.Range(0, Stat.myPatterns.Count)];
                yield return StartCoroutine(selectedPattern.Execute(this));
                yield return new WaitForSeconds(selectedPattern.nextPatternDelay);

                _isAttacking = false;
            }
            yield return null;
        }
    }

    public void FireBullet(Vector2 direction, float speed)
    {
        if (Stat.bossBulletPrefab == null) return;

        GameObject bullet = Managers.Resource.Instantiate(Stat.bossBulletPrefab);
        bullet.GetComponent<BossBulletController>().SetBullet(Stat.Damage.TotalValue, firePoint.position, direction, speed);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player == null) return;

            player.OnDamage(Stat.Damage.TotalValue);
        }
    }

    // =========================================================
    // 데미지 처리 및 사망 로직
    // =========================================================
    public void OnDamage(float damage, bool isCrit = false, GameObject attacker = null)
    {
        if (_isDead) return;

        // [추가] 감전 상태 터트리기
        if (Status != null && Status.HasShockDebuff)
        {
            damage *= 1.5f;
            Status.ShockDebuffOff();
            Managers.Effect.Play(EffectType.Meteor_ShockHit, transform.position);
            Managers.Sound.Play(Define.SoundID.Sfx_Lightning_Hit);
        }

        currentHp -= damage;

        OnHpChanged?.Invoke(currentHp, Stat.MaxHp.TotalValue);

        PlayHitFlash();

        Vector3 textPos = transform.position + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0.5f, 0);
        GameObject go = Managers.Resource.Instantiate("DamageText");
        DamageText damageText = go.GetOrAddComponent<DamageText>();
        if (damageText != null)
        {
            damageText.Init(textPos, Mathf.FloorToInt(damage), isCrit);
        }

        Managers.Sound.Play(Define.SoundID.Sfx_NormalBulletHit);

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;

        // [추가] 보스가 죽었을 때 얼음 장판 마커가 있다면 터트립니다.
        if (Status != null && Status.hasIcePuddleMark)
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

        StopAllCoroutines();

        Debug.Log("보스 처치 완료!");

        Managers.Game.ChangeGameState(Define.GameState.GameClear);
        Managers.Resource.Destroy(gameObject);
    }

    public void SetColor(Color color)
    {
        if (_meshRenderer != null)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColorTint", color);
            _meshRenderer.SetPropertyBlock(mpb);
        }
    }

    public void ReturnColor()
    {
        if (_meshRenderer != null)
        {
            SetColor(Color.white);
        }
    }

    public void PlayHitFlash()
    {
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(CoHitFlash());
    }

    private IEnumerator CoHitFlash()
    {
        if (_spriteRenderer == null) yield break;

        _spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(FlashColorID, new Color(3f, 3f, 3f, 1f));
        _spriteRenderer.SetPropertyBlock(_mpb);

        yield return new WaitForGameTime(0.1f);

        ResetFlashColor();
    }

    private void ResetFlashColor()
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(FlashColorID, Color.white);
            _spriteRenderer.SetPropertyBlock(_mpb);
        }
    }

    // ==========================================================
    // IStatusTarget 인터페이스 구현부 (StatusEffectReceiver에서 통제)
    // ==========================================================
    public void TakeStatusDamage(float damage)
    {
        OnDamage(damage); // 화상/독 같은 틱 데미지 처리
    }

    public void AddSpeedMultiplier(float multiplier)
    {
        _speedMultiplier += multiplier; // 속도 배율 더하기 (ex. 슬로우 적용)
    }

    public void SubSpeedMultiplier(float multiplier)
    {
        _speedMultiplier -= multiplier; // 속도 배율 빼기 (ex. 슬로우 해제)
    }

    public void SetForceZeroSpeed(bool isZero)
    {
        _isForceZeroSpeed = isZero; // 빙결 시 강제 정지 ON/OFF
    }

    public void SetStatusColor(Color color)
    {
        SetColor(color); // 보스의 _meshRenderer 기반 색상 변경 함수 호출
    }

    public void ResetStatusColor()
    {
        ReturnColor(); // 원래 색상 복구 함수 호출
    }
}