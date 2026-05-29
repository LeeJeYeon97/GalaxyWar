using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;

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

    public void Init(BossStat stat,Vector2 startPos, GameObject target)
    {
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
        if (_meshRenderer == null) yield break;
        SetColor(new Color(5f, 5f, 5f, 1f));
        yield return new WaitForGameTime(0.1f);
        ReturnColor();
    }
}
