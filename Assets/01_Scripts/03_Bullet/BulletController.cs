using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static Define;


public class BulletController : MonoBehaviour
{

    [field: SerializeField] public BaseBulletStat Stat { get; private set; }
    [field: SerializeField] public Collider2D Collider { get; private set; }
    [field: SerializeField] public Rigidbody2D Rb { get; private set; }

    [SerializeField] private Vector2 _shotDir;

    private BulletParticle _particle;


    // 바뀌는 값은 여기서 선언
    [field: SerializeField] public int currentBounceCount { get; private set; }
    [field: SerializeField] public int currentPierceCount { get; private set; }
 
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

    }
    private void OnDisable()
    {
        if(Stat != null)
        {
            Stat.behavior.OnRelease(this);
        }
        Managers.Game.RemoveActiveObject(this);
    }
    private void FixedUpdate()
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
        currentBounceCount = Mathf.FloorToInt(stat.bounceCount.TotalValue);
        //currentPierceCount = Mathf.FloorToInt(stat.pierceCount.TotalValue);
        
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
        _particle?.SpawnHit(collision.transform.position,Vector2.zero,Stat);
        MeteorController meteor = collision.gameObject.GetComponent<MeteorController>();

        if (meteor != null)
        {
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
        _shotDir = dragVector.normalized * Stat.speed.TotalValue;

        SetPhysicsState(false);
        Rb.AddForce(_shotDir, ForceMode2D.Impulse);

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
            Managers.Pool.Release(gameObject);
        }
        // 필요하다면 여기서 0 이하가 됐을 때의 로직을 추가할 수도 있음
    }

    // [추가] 튕김 처리를 요청할 때 사용하는 함수
    public void DecreaseBounceCount()
    {
        currentBounceCount--;

        if (currentBounceCount < 0)
        {
            // 반납처리 필요
            Managers.Pool.Release(gameObject);
        }
    }
    public void SetRigidVelocity(Vector2 velocity)
    {
        Rb.linearVelocity = velocity;
    }
    //private IEnumerator CoDropFireTrail()
    //{
    //    Vector2 lastDropPos = transform.position;
    //    float dropDistance = 1.0f; // 1.0 거리마다 장판 1개 생성

    //    while (gameObject.activeSelf)
    //    {
    //        if (Vector2.Distance(lastDropPos, transform.position) >= dropDistance)
    //        {
    //            // 풀에서 파이어존(장판)을 꺼내서 총알 위치에 배치
    //            GameObject fireZone = Managers.Pool.Get("FireZone").gameObject;
    //            if (fireZone != null)
    //            {
    //                fireZone.transform.position = transform.position;
    //                // (FireZone 스크립트 내부의 OnEnable에서 3초 뒤 자동 반납되도록 구현되어 있어야 합니다)
    //            }

    //            lastDropPos = transform.position;
    //        }
    //        yield return null;
    //    }
    //}
   
}
