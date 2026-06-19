using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using static Define;

public class BossController : BaseController, IDamageable
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

    public void Init(BossStat stat,Vector2 startPos, GameObject target)
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Awake에서 딱 한 번만 할당합니다.
        _mpb = new MaterialPropertyBlock();
        // 타겟을 계속 따라가도록 설정
        Stat = stat;
        currentHp = Stat.MaxHp.TotalValue;
        transform.localPosition = startPos;

        // 타겟 방향으로 방향 지정
        attackTarget = target;

        _isDead = false;

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
                    // 현재 속도와 회전값을 안전하게 저장해둡니다.
                    _savedVelocity = Rb.linearVelocity;
                    _savedAngularVelocity = Rb.angularVelocity;

                    // 속도를 0으로 묶고, 물리 연산을 강제로 정지시킵니다.
                    Rb.linearVelocity = Vector2.zero;
                    Rb.angularVelocity = 0f;
                    Rb.bodyType = RigidbodyType2D.Kinematic;
                }
            }
            // 일시 정지 중이므로 아래의 이동 및 회전 로직을 무시하고 바로 리턴합니다.
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
                    // 보스가 원래 Dynamic이었다면 다시 풀어줍니다.
                    // (만약 원래부터 Kinematic만 쓰는 보스라면 아래 줄은 지우셔도 됩니다)
                    Rb.bodyType = RigidbodyType2D.Dynamic;

                    // 저장해둔 속도를 돌려줍니다.
                    Rb.linearVelocity = _savedVelocity;
                    Rb.angularVelocity = _savedAngularVelocity;
                }
            }
        }
        // 1. 타겟이 없거나 보스가 죽었으면 추적 중지
        if (_isDead || attackTarget == null) return;

        // 2. 보스의 이동 속도 가져오기
        float moveSpeed = Stat.Speed.TotalValue;

        // 3. 타겟을 향해 '일정한 속도(moveSpeed)'로 계속 이동
        transform.position = Vector2.MoveTowards(
            transform.position,
            attackTarget.transform.position,
            moveSpeed * Time.deltaTime
        );

        //  4. 타겟을 바라보도록 부드럽게 회전하기 
        // 타겟을 향하는 방향 벡터를 구합니다.
        Vector2 direction = attackTarget.transform.position - transform.position;

        // 방향 벡터를 바탕으로 회전해야 할 각도(Z축)를 계산합니다.
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 보스 이미지 원본이 어느 방향을 보고 그려졌는지에 따라 각도 보정이 필요합니다!
        // (보통 유니티 2D는 오른쪽이 0도 기준입니다. 만약 보스가 기본적으로 위를 보고 그려졌다면 -90f를 해줘야 합니다.)
        float offsetAngle = -90f; // ⬅️ 보스 이미지가 엉뚱한 곳을 본다면 이 숫자를 0, 90, -90, 180 등으로 바꿔보세요!

        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle + offsetAngle);

        // 즉시 휙! 돌지 않고, 보스 특유의 묵직한 느낌으로 부드럽게 회전시킵니다. (5f는 회전 속도)
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
    }
    private IEnumerator BossPatternLoop()
    {
        yield return new WaitForSeconds(2f); // 등장 대기 시간

        while (!_isDead)
        {
            // [핵심] 게임이 Pause 상태라면, 다시 Play 상태가 될 때까지 기다립니다.
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                yield return new WaitUntil(() => Managers.Game.currentGameState == GameState.Playing);
            }
            // 패턴 리스트가 비어있지 않고, 공격 중이 아닐 때
            if (!_isAttacking && Stat.myPatterns.Count > 0)
            {
                _isAttacking = true;

                // 1. 내가 가진 패턴 중 무작위로 하나 뽑기
                BossPatternSO selectedPattern = Stat.myPatterns[UnityEngine.Random.Range(0, Stat.myPatterns.Count)];

                // 2.  전략 패턴의 핵심: 어떤 패턴이든 동일한 Execute 함수로 실행!
                // (StartCoroutine으로 감싸서 SO가 반환하는 IEnumerator를 실행해줍니다)
                yield return StartCoroutine(selectedPattern.Execute(this));

                // 3. 패턴에 설정된 휴식 시간만큼 대기
                yield return new WaitForSeconds(selectedPattern.nextPatternDelay);

                _isAttacking = false;
            }
            yield return null;
        }
    }
    public void FireBullet(Vector2 direction, float speed)
    {
        if (Stat.bossBulletPrefab == null) return;
        
        // 실제 출시 버전에서는 ResourceManager의 Instantiate(오브젝트 풀링)를 추천합니다!
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
    public void OnDamage(float damage, bool isCrit = false)
    {
        if (_isDead) return;

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

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;

        // 실행 중이던 모든 탄막 코루틴을 강제로 멈춥니다.
        StopAllCoroutines();

        Debug.Log("보스 처치 완료!");

        // TODO: 화려한 연쇄 폭발 파티클 소환
        // 파티클 다 되면

        // GameManager에게 클리어 판정 전달!
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
        if (_meshRenderer != null )// && _controller.Status != null
        {
            SetColor(Color.white);
        }
    }
    //public Color GetCurrentStatusColor()
    //{
    //    if (HasAuraBuff) return Color.yellow;
    //    if (_burnCoroutine != null) return new Color(1f, 0.4f, 0f);
    //    if (_freezeCoroutine != null) return new Color(0.2f, 0.8f, 1f);
    //    if (_slowCoroutine != null) return new Color(0.5f, 0.8f, 1f);
    //    return Color.white;
    //}
    public void PlayHitFlash()
    {
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(CoHitFlash());
    }

    private IEnumerator CoHitFlash()
    {
        if (_spriteRenderer == null) yield break;

        //  플래시 ON (HDR 컬러로 눈부시게!)
        _spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(FlashColorID, new Color(3f, 3f, 3f, 1f));
        _spriteRenderer.SetPropertyBlock(_mpb);

        yield return new WaitForGameTime(0.1f);

        //  플래시 OFF (원상 복구)
        ResetFlashColor();
    }
    private void ResetFlashColor()
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(FlashColorID, Color.white); // 플레이어 셰이더 기준 기본 중립 색상
            _spriteRenderer.SetPropertyBlock(_mpb);
        }
    }
}
