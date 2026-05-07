using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static Define;


public class BulletController : BaseController
{

    [field: SerializeReference] public BaseBulletStat Stat { get; private set; }
    [field: SerializeField] public Collider2D Collider { get; private set; }
    [field: SerializeField] public Rigidbody2D Rb { get; private set; }

    [SerializeField] private Vector2 _shotDir;

    public BulletParticle BulletParticle { get; private set; }


    //  2. 물리 정지 상태를 기억할 변수들
    private bool _isPhysicsPaused = false;
    private Vector2 _savedVelocity;

    // 바뀌는 값은 여기서 선언
    [field: SerializeField] public int currentBounceCount { get; private set; }
    [field: SerializeField] public int currentPierceCount { get; set; }

    public float CurDamage;
    public void Awake()
    {
        if(Rb == null)
        {
            Rb = Util.GetOrAddComponent<Rigidbody2D>(gameObject);
        }
        if(Collider == null)
        {
            Collider = Util.GetOrAddComponent<Collider2D>(gameObject);
        }
        Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Collider.isTrigger = true;

        BulletParticle = Util.GetOrAddComponent<BulletParticle>(gameObject);
    }

    private void OnEnable()
    {
        Managers.Game.AddActiveObject(this);

        Managers.Event.Subscribe<int>(ActionEvent.BulletBounceCountUp,UpdateBounceCount);
    }
    private void OnDisable()
    {
        if(Stat != null)
        {
            Stat.behavior.OnRelease(this);
        }
        Managers.Event.UnSubscribe<int>(ActionEvent.BulletBounceCountUp, UpdateBounceCount);
        Managers.Game.RemoveActiveObject(this);

    }
    protected override void FixedUpdate()
    {
        bool isPaused = (Managers.Game.currentGameState == GameState.Pause);
        bool isGameOver = (Managers.Game.currentGameState == GameState.GameOver);

        // [얼리기] 정지 또는 게임오버인데 아직 물리 스위치가 켜져 있다면
        if ((isPaused || isGameOver) && !_isPhysicsPaused)
        {
            // 현재 날아가던 속도를 백업하고 물리 엔진을 끕니다.
            _savedVelocity = Rb.linearVelocity;
            Rb.linearVelocity = Vector2.zero;
            Rb.simulated = false;
            _isPhysicsPaused = true;
        }
        // [녹이기]  수정된 부분: 정지도 아니고 게임오버도 아닐 때만 풀어줍니다.
        else if (!isPaused && !isGameOver && _isPhysicsPaused)
        {
            Rb.simulated = true;
            Rb.linearVelocity = _savedVelocity;
            _isPhysicsPaused = false;
        }

        // 일시정지 중이면 아래 부모의 FixedUpdate 및 OnFixedUpdate(WallBounce, Rotate)를 실행하지 않습니다.
        if (isPaused || isGameOver) return;

        base.FixedUpdate();
    }

    //  5. 실제 로직은 OnFixedUpdate 안에서 안전하게 실행
    protected override void OnFixedUpdate()
    {
        WallBounce();
        Rotate();
    }

    private void WallBounce()
    {
        // 일시정지 중이면 연산 자체를 건너뜁니다.
        if (_isPhysicsPaused) return;
        if (Stat.type == Define.BulletType.HomingBullet) return;

        // 관통탄(Trigger)일 때만 수동 튕기기 체크
        if (Collider.isTrigger)
        {
            // 1. 다음 프레임에 이동할 거리 계산 (속도 * 시간)
            float moveDistance = Stat.speed.TotalValue * Time.fixedDeltaTime;

            // 2. 레이캐스트 발사 (현재 위치에서 이동 방향으로 moveDistance보다 살짝 더 길게 쏨)
            // 1.2~1.5 정도를 곱해줘야 벽에 박히기 전에 미리 튕깁니다.
            int wallLayerMask = LayerMask.GetMask("Wall");
            RaycastHit2D hit = Physics2D.Raycast(Rb.position, _shotDir, moveDistance * 1.5f, wallLayerMask);

            if (hit.collider != null)
            {
                // 벽을 만났다!
                // 3. 반사각 계산
                _shotDir = Vector2.Reflect(_shotDir, hit.normal).normalized;

                // 4. 즉시 위치 보정 (벽 안으로 파고드는 것 방지)
                // 충돌 지점에서 법선 방향으로 살짝 띄워줍니다.
                Rb.position = hit.point + (hit.normal * 0.05f);

                // 5. 속도 재설정
                Rb.linearVelocity = _shotDir * Stat.speed.TotalValue;

                BulletParticle?.SpawnHit(hit.point, Vector2.zero, Stat);
                // 바운스 카운트 감소
                DecreaseBounceCount();
            }
        }
    }
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
    public void SetBullet(BaseBulletStat stat)
    {
        if(stat == null)
        {
            Debug.LogError("불릿 스탯이 null입니다 SetBullet()");
        }

        // 스위치 끄기
        if (gameObject.activeSelf == true)
        {
            gameObject.SetActive(false);
        }
        
        Stat = stat;
        CurDamage = Stat.damage.TotalValue;
        SetPhysicsState(true); // 대기 중엔 물리 끄기
        
        // 각 탄환별로 초기화 시 실행시킬 로직 실행
        Stat.behavior.OnInit(this,Stat);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        MeteorController meteor = collision.gameObject.GetComponent<MeteorController>();
        if (meteor == null) return;

        // 1. 공통 처리: 데미지 주고 파티클 생성
        Vector2 hitPoint = collision.ClosestPoint(transform.position);
        BulletParticle?.SpawnHit(hitPoint, Vector2.zero, Stat);

        CalculateDamage(meteor , CurDamage);
        Stat.behavior.OnHit(this, collision.gameObject, Stat);

        // 2. 능력치에 따른 분기 처리 (관통 -> 도탄 -> 소멸 순서)
        if (currentPierceCount > 0)
        {
            // [관통탄] 관통 횟수가 남아있다면 튕기지 않고 그냥 지나갑니다.
            currentPierceCount--;
        }
        else if (currentBounceCount > 0)
        {
            // [일반탄/도탄] 관통은 안 되는데 튕길 횟수가 남아있다면 튕겨 나갑니다.
            ReflectFromMeteor(collision);
            DecreaseBounceCount();
        }
        else
        {
            // [소멸] 관통도 안 되고 더 튕길 수도 없다면 삭제합니다.
            Managers.Resource.Destroy(gameObject);
        }
    }

    public void CalculateDamage(MeteorController targetMeteor, float baseDamage)
    {
        if (targetMeteor == null || baseDamage <= 0) return;

        // 1. 플레이어 스탯 가져오기
        float critChance = Managers.Game._player.Stat.criticalChance.TotalValue;
        float critDamageMultiplier = Managers.Game._player.Stat.criticalDamageRate.TotalValue;

        // 2. 주사위 굴리기 (여기서 개별 타격마다 크리티컬이 톡톡 터짐!)
        bool isCrit = UnityEngine.Random.value <= critChance;
        float finalDmg = isCrit ? (baseDamage * critDamageMultiplier) : baseDamage;

        // 3. 계산된 최종 데미지와 크리티컬 여부를 메테오에게 전달
        targetMeteor.OnDamage(finalDmg, isCrit);
    }
    private void ReflectFromMeteor(Collider2D meteorCollider)
    {
        // 메테오의 중심에서 총알 위치로 향하는 방향을 법선(Normal)으로 사용합니다.
        // (메테오가 원형에 가깝기 때문에 가장 자연스러운 반사각이 나옵니다.)
        Vector2 normal = ((Vector2)transform.position - (Vector2)meteorCollider.transform.position).normalized;

        // 유니티의 Reflect 함수를 이용해 반사 방향을 구합니다.
        _shotDir = Vector2.Reflect(_shotDir.normalized, normal).normalized;

        // 물리 엔진에 새로운 속도를 즉시 반영합니다.
        Rb.linearVelocity = _shotDir * Stat.speed.TotalValue;

        // 팁: 메테오 안으로 파고드는 것을 방지하기 위해 위치를 살짝 밀어줍니다.
        transform.position = (Vector2)transform.position + (normal * 0.1f);
    }
    // 발사
    public void Shot(Vector2 dragVector, Vector2 shotPos)
    {
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }

        transform.position = shotPos;
        _shotDir = dragVector.normalized * Stat.speed.TotalValue;

        SetPhysicsState(false);
        Rb.AddForce(_shotDir, ForceMode2D.Impulse);

        currentBounceCount = Mathf.FloorToInt(Stat.bounceCount.TotalValue);
        BulletParticle.SpawnShot(dragVector,shotPos);
        Stat.behavior.OnShot(this);
    }
    
    // 발사대에 있을 때 드래그중이면 Kinematic으로
    public void SetPhysicsState(bool isKinematic)
    {
        Rb.bodyType = isKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        if (isKinematic)
        {
            Rb.linearVelocity = Vector2.zero;
            Rb.angularVelocity = 0f;
        }
    }
    public void DecreasePierceCount()
    {
        currentPierceCount--;
        if(currentPierceCount < 0)
        {
            // 반납처리 필요
            Managers.Resource.Destroy(gameObject);
        }
        // 필요하다면 여기서 0 이하가 됐을 때의 로직을 추가할 수도 있음
    }
    public void UpdateBounceCount(int count)
    {
        if (count <= 0) return;

        int maxBounce = (int)Stat.bounceCount.TotalValue;
        if (currentBounceCount >= maxBounce) return;

        // 마법의 코드: (현재값 + 더할값)과 (최댓값) 중에서 '더 작은 것'을 내 현재값으로 설정합니다!
        // 이렇게 하면 알아서 최댓값을 넘어가지 않게 딱 잘라줍니다.
        currentBounceCount = Mathf.Min(currentBounceCount + count, maxBounce);
    }
    // [추가] 튕김 처리를 요청할 때 사용하는 함수
    public void DecreaseBounceCount()
    {
        currentBounceCount--;

        if (currentBounceCount < 0)
        {
            // 반납처리 필요
            Managers.Resource.Destroy(gameObject);
        }
    }
}
