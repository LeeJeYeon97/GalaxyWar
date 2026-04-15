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

    private BulletParticle _particle;


    //  2. 물리 정지 상태를 기억할 변수들
    private bool _isPhysicsPaused = false;
    private Vector2 _savedVelocity;

    // 바뀌는 값은 여기서 선언
    [field: SerializeField] public int currentBounceCount { get; private set; }
    [field: SerializeField] public int currentPierceCount { get; set; }
 
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

        _particle = Util.GetOrAddComponent<BulletParticle>(gameObject);


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
        bool isGamePaused = (Managers.Game.currentGameState == GameState.Pause);

        // [얼리기] 방금 일시정지가 되었다면?
        if (isGamePaused && !_isPhysicsPaused)
        {
            // 현재 날아가던 속도와 방향을 백업해둡니다.
            _savedVelocity = Rb.linearVelocity;

            // 미끄러짐 방지를 위해 속도를 0으로 덮어씌우고 물리 엔진 스위치를 끕니다.
            Rb.linearVelocity = Vector2.zero;
            Rb.simulated = false;
            _isPhysicsPaused = true;
        }
        // [녹이기] 방금 일시정지가 풀렸다면?
        else if (!isGamePaused && _isPhysicsPaused)
        {
            // 물리 엔진 스위치를 켜고, 아까 백업해둔 속도를 다시 넣어줍니다.
            Rb.simulated = true;
            Rb.linearVelocity = _savedVelocity;
            _isPhysicsPaused = false;
        }

        //  4. 부모(BaseController)의 FixedUpdate 호출!
        // (일시정지 중이면 여기서 막히고, 아니면 아래 OnFixedUpdate가 실행됩니다.)
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


                _particle?.SpawnHit(hit.point, Vector2.zero, Stat);
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

        SetPhysicsState(true); // 대기 중엔 물리 끄기
        
        // 각 탄환별로 초기화 시 실행시킬 로직 실행
        Stat.behavior.OnInit(this);
    }
    // 충돌
    private void OnCollisionEnter2D(Collision2D collision)
    {
        _particle?.SpawnHit(collision.contacts[0].point, collision.contacts[0].normal,Stat);

        MeteorController meteor = collision.gameObject.GetComponent<MeteorController>();

        // 바운스 횟수 까기
        // 벽에 닿아도 깔건지 운석에만 맞았을 때 깔건지 고민좀 해볼것
        DecreaseBounceCount();

        if (meteor != null)
        {
            // 데미지 주기
            meteor.OnDamage(Stat.damage.TotalValue);
            // 가지고 있는 능력 실행

            Stat.behavior.OnHit(this, collision.gameObject);
        }

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 관통탄 or 관통 열려있을때 버스트탄

        MeteorController meteor = collision.gameObject.GetComponent<MeteorController>();

        if (meteor != null)
        {
            _particle?.SpawnHit(meteor.transform.position, Vector2.zero, Stat);
            // 데미지 주고 관통횟수 감소
            meteor.OnDamage(Stat.damage.TotalValue);
            DecreasePierceCount();
            // 바운스 횟수는 WallBounce에서 까줌

            
            // 가지고 있는 능력 실행
            Stat.behavior.OnHit(this,collision.gameObject);
        }
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
        _particle.SpawnShot(dragVector,shotPos);
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
