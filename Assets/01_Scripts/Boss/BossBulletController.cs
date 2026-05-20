using UnityEngine;
using static Define;

public class BossBulletController : BaseController
{
    [field: SerializeField] public Collider2D Collider { get; private set; }
    [field: SerializeField] public Rigidbody2D Rb { get; private set; }

    [SerializeField] private Vector2 _shotDir;

    public BulletParticle BulletParticle { get; private set; }

    //  2. 물리 정지 상태를 기억할 변수들
    private bool _isPhysicsPaused = false;
    private Vector2 _savedVelocity;

    public float CurDamage;
    public void Awake()
    {
        if (Rb == null)
        {
            Rb = Util.GetOrAddComponent<Rigidbody2D>(gameObject);
        }
        if (Collider == null)
        {
            Collider = Util.GetOrAddComponent<Collider2D>(gameObject);
        }

        Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Collider.isTrigger = true;

        BulletParticle = Util.GetOrAddComponent<BulletParticle>(gameObject);
    }

    private void OnEnable()
    {
    }
    private void OnDisable()
    {
    }
    protected override void FixedUpdate()
    {
        bool isPaused = (Managers.Game.currentGameState == GameState.Pause);
        bool isGameOver = (Managers.Game.currentGameState == GameState.GameOver);

        bool isGameClear = (Managers.Game.currentGameState == GameState.GameClear);

        // [얼리기] 정지 또는 게임오버인데 아직 물리 스위치가 켜져 있다면
        if ((isPaused || isGameOver || isGameClear) && !_isPhysicsPaused)
        {
            // 현재 날아가던 속도를 백업하고 물리 엔진을 끕니다.
            _savedVelocity = Rb.linearVelocity;
            Rb.linearVelocity = Vector2.zero;
            Rb.simulated = false;
            _isPhysicsPaused = true;
        }
        // [녹이기]  수정된 부분: 정지도 아니고 게임오버도 아닐 때만 풀어줍니다.
        else if (!isPaused && !isGameOver && !isGameClear && _isPhysicsPaused)
        {
            Rb.simulated = true;
            Rb.linearVelocity = _savedVelocity;
            _isPhysicsPaused = false;
        }

        // 일시정지 중이면 아래 부모의 FixedUpdate 및 OnFixedUpdate(WallBounce, Rotate)를 실행하지 않습니다.
        if (isPaused || isGameOver || isGameClear) return;

        base.FixedUpdate();
    }

    //  5. 실제 로직은 OnFixedUpdate 안에서 안전하게 실행
    protected override void OnFixedUpdate()
    {
        //WallBounce();
        Rotate();
    }

    //private void WallBounce()
    //{
    //    // 일시정지 중이면 연산 자체를 건너뜁니다.
    //    if (_isPhysicsPaused) return;
    //    if (Stat.type == Define.BulletType.HomingBullet) return;

    //    // 관통탄(Trigger)일 때만 수동 튕기기 체크
    //    if (Collider.isTrigger)
    //    {
    //        // 1. 다음 프레임에 이동할 거리 계산 (속도 * 시간)
    //        float moveDistance = Stat.speed.TotalValue * Time.fixedDeltaTime;

    //        // 2. 레이캐스트 발사 (현재 위치에서 이동 방향으로 moveDistance보다 살짝 더 길게 쏨)
    //        // 1.2~1.5 정도를 곱해줘야 벽에 박히기 전에 미리 튕깁니다.
    //        int wallLayerMask = LayerMask.GetMask("Wall");
    //        RaycastHit2D hit = Physics2D.Raycast(Rb.position, _shotDir, moveDistance * 1.5f, wallLayerMask);

    //        if (hit.collider != null)
    //        {
    //            // 벽을 만났다!
    //            // 3. 반사각 계산
    //            _shotDir = Vector2.Reflect(_shotDir, hit.normal).normalized;

    //            // 4. 즉시 위치 보정 (벽 안으로 파고드는 것 방지)
    //            // 충돌 지점에서 법선 방향으로 살짝 띄워줍니다.
    //            Rb.position = hit.point + (hit.normal * 0.05f);

    //            // 5. 속도 재설정
    //            Rb.linearVelocity = _shotDir * Stat.speed.TotalValue;

    //            BulletParticle?.SpawnHit(hit.point, Vector2.zero, Stat);
    //            // 바운스 카운트 감소
    //            DecreaseBounceCount();
    //        }
    //    }
    //}
    private void Rotate()
    {
        // 정지 상태가 아닐 때만 회전 처리
        if (Rb.bodyType == RigidbodyType2D.Dynamic && Rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            // 1. 현재 물리 엔진이 계산한 속도의 방향을 가져옴
            Vector2 currentDir = Rb.linearVelocity.normalized;

            // 2. 방향을 각도로 변환
            float angle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;

            // 3. 회전 적용 (파티클이 뒤로 뿜어져 나오는 구조라면 -90f 유지)
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        }
    }
    public void SetBullet(float Damage, Vector2 startPos, Vector2 dir, float speed)
    {

        CurDamage = Damage;
        transform.position = startPos;
        transform.rotation = Quaternion.identity;
        Rb.linearVelocity = dir * speed;

        // 총알 이미지가 날아가는 방향을 바라보도록 회전
        //float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        //bullet.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 상대가 누구든, '데미지를 받을 수 있는 녀석(IDamageable)'인지 한 번만 검사합니다!
        IDamageable target = collision.gameObject.GetComponent<IDamageable>();

        // 타겟이 아니면(벽, 아이템 등) 무시!
        if (target == null) return;

        // 1. 공통 처리: 데미지 주고 파티클 생성
        Vector2 hitPoint = collision.ClosestPoint(transform.position);
        BulletParticle?.SpawnHit(hitPoint, Vector2.zero);

        target.OnDamage(CurDamage, false);

        // [소멸] 관통도 안 되고 더 튕길 수도 없다면 삭제합니다.
        Managers.Resource.Destroy(gameObject);
    }

    //public void CalculateDamage(IDamageable target, float baseDamage)
    //{
    //    if (target == null || baseDamage <= 0)
    //    {
    //        return;
    //    }
    //
    //    // 1. 플레이어 스탯 가져오기
    //    float critChance = Managers.Game._player.Stat.criticalChance.TotalValue;
    //    float critDamageMultiplier = Managers.Game._player.Stat.criticalDamageRate.TotalValue;
    //
    //    // 2. 0~100 스케일 주사위 굴리기 
    //    // UnityEngine.Random.Range(min, max)를 사용해서 0.0f부터 100.0f 사이의 난수를 뽑습니다.
    //    bool isCrit = UnityEngine.Random.Range(0f, 100f) <= critChance;
    //    float finalDmg = isCrit ? (baseDamage * critDamageMultiplier) : baseDamage;
    //
    //    target.OnDamage(finalDmg, isCrit);
    //}
    //private void ReflectFromMeteor(Collider2D meteorCollider)
    //{
    //    // 메테오의 중심에서 총알 위치로 향하는 방향을 법선(Normal)으로 사용합니다.
    //    // (메테오가 원형에 가깝기 때문에 가장 자연스러운 반사각이 나옵니다.)
    //    Vector2 normal = ((Vector2)transform.position - (Vector2)meteorCollider.transform.position).normalized;
    //
    //    // 유니티의 Reflect 함수를 이용해 반사 방향을 구합니다.
    //    _shotDir = Vector2.Reflect(_shotDir.normalized, normal).normalized;
    //
    //    // 물리 엔진에 새로운 속도를 즉시 반영합니다.
    //    Rb.linearVelocity = _shotDir * Stat.speed.TotalValue;
    //
    //    // 팁: 메테오 안으로 파고드는 것을 방지하기 위해 위치를 살짝 밀어줍니다.
    //    transform.position = (Vector2)transform.position + (normal * 0.1f);
    //}
}
