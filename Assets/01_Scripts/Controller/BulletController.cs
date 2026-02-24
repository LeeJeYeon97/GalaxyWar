using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.AppUI.Core;
using UnityEngine;
using UnityEngine.Rendering;
using static Define;


public class BulletController : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D _rb;
    [SerializeField]
    private BulletStat _stat;
    
    private Vector2 _shotDir;
    private Collider2D _collider;
    // 바뀌는 값은 여기서 선언
    public bool canSplit { get; private set; }
    public int currentBounceCount { get; private set; }
    public int currentPierceCount { get; private set; }
    #region Particle변수

    #endregion

    public void Awake()
    {
        if(_rb == null) _rb = Util.GetOrAddComponent<Rigidbody2D>(gameObject);
        _collider = Util.GetOrAddComponent<Collider2D>(gameObject);
    }
    private void OnEnable()
    {
        Managers.Game.AddActiveObject(this);

    }
    private void OnDisable()
    {
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
        if (_collider.isTrigger)
        {
            // 1. 다음 프레임에 이동할 거리 계산 (속도 * 시간)
            float moveDistance = _stat.speed.TotalValue * Time.fixedDeltaTime;

            // 2. 레이캐스트 발사 (현재 위치에서 이동 방향으로 moveDistance보다 살짝 더 길게 쏨)
            // 1.2~1.5 정도를 곱해줘야 벽에 박히기 전에 미리 튕깁니다.
            int wallLayerMask = LayerMask.GetMask("Wall");
            RaycastHit2D hit = Physics2D.Raycast(_rb.position, _shotDir, moveDistance * 1.5f, wallLayerMask);

            if (hit.collider != null)
            {
                // ★ 벽을 만났다!
                // 3. 반사각 계산
                _shotDir = Vector2.Reflect(_shotDir, hit.normal).normalized;

                // 4. 즉시 위치 보정 (벽 안으로 파고드는 것 방지)
                // 충돌 지점에서 법선 방향으로 살짝 띄워줍니다.
                _rb.position = hit.point + (hit.normal * 0.05f);

                // 5. 속도 재설정
                _rb.linearVelocity = _shotDir * _stat.speed.TotalValue;

                // 바운스 카운트 감소
                DecreaseBounceCount();
            }
        }
    }
    private void Rotate()
    {
        // 정지 상태가 아닐 때만 회전 처리
        if (_rb.bodyType == RigidbodyType2D.Dynamic && _rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            // 1. 현재 물리 엔진이 계산한 속도의 방향을 가져옴
            Vector2 currentDir = _rb.linearVelocity.normalized;

            // 2. 방향을 각도로 변환
            float angle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;

            // 3. 회전 적용 (파티클이 뒤로 뿜어져 나오는 구조라면 -90f 유지)
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        }
    }
    public void SetBullet(BulletStat stat)
    {
        if (gameObject.activeSelf == true)
        {
            gameObject.SetActive(false);
        }
        
        _stat = stat;
        currentBounceCount = Mathf.FloorToInt(stat.bounceCount.TotalValue);
        currentPierceCount = Mathf.FloorToInt(stat.pierceCount.TotalValue);
        canSplit = true;
        
        // ★ 관통탄이면 Trigger를 켬 / 버스트탄인데 관통 기능이 있으면 이것도 켬
        if (_stat.type == Define.BulletType.PierceBullet ||
            (_stat.type == Define.BulletType.BurstBullet && Managers.Ability.GetCurrentLevel(AbilityType.ActivatePierceBullet) > 0))
        {
            _collider.isTrigger = true;
        }
        else
        {
            _collider.isTrigger = false;
        }

        SetPhysicsState(true); // 대기 중엔 물리 끄기
        _rb.angularVelocity = 0f;
    }
    public void SetSplit()
    {
        canSplit = false;
    }
    // 충돌
    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayHitEffect(collision.contacts[0].point, collision.contacts[0].normal);

        MeteorController meteor = collision.gameObject.GetComponent<MeteorController>();

        // 바운스 횟수 까기
        // 벽에 닿아도 깔건지 운석에만 맞았을 때 깔건지 고민좀 해볼것
        DecreaseBounceCount();

        if (meteor != null)
        {
            // 파라미터 팩 만들기
            AbilityExecuteParams param = new AbilityExecuteParams
            {
                stat = _stat,
                bullet = this,
                meteor = meteor,
                collision = collision, // 충돌 정보 통째로 전달
                trigger = null,
                incomingDirection = _rb.linearVelocity.normalized, // 들어온 방향
                shotDir = _shotDir
            };
            // 데미지 주기
            meteor.OnDamage(_stat.damage.TotalValue);
            // 가지고 있는 능력 실행
            _stat.ability.Execute(param);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 관통탄 or 관통 열려있을때 버스트탄
        PlayHitEffect(gameObject.transform.position,Vector2.zero);

        MeteorController meteor = collision.gameObject.GetComponent<MeteorController>();

        if (meteor != null)
        {
            // 데미지 주고 관통횟수 감소
            meteor.OnDamage(_stat.damage.TotalValue);
            DecreasePierceCount();
            // 바운스 횟수는 WallBounce에서 까줌

            // 파라미터 팩 만들기
            AbilityExecuteParams param = new AbilityExecuteParams
            {
                stat = _stat,
                bullet = this,
                meteor = meteor,
                collision = null,
                trigger = collision, // 충돌 정보 통째로 전달
                incomingDirection = _rb.linearVelocity.normalized, // 들어온 방향
                shotDir = _shotDir
            };
            // 가지고 있는 능력 실행
            _stat.ability.Execute(param);
        }
    }
    // 발사
    public void Shot(Vector2 dragVector)
    {
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }
        _shotDir = dragVector * _stat.speed.TotalValue;

        SetPhysicsState(false);
        _rb.AddForce(_shotDir, ForceMode2D.Impulse);

    }
    
    // 발사대에 있을 때 드래그중이면 Kinematic으로
    public void SetPhysicsState(bool isKinematic)
    {
        _rb.bodyType = isKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        if (isKinematic) _rb.linearVelocity = Vector2.zero;
    }
    // 히트 이펙트를 재생하는 별도 함수
    private void PlayHitEffect(Vector2 hitPos, Vector2 hitNormal)
    {
        // 1. 풀에서 이펙트 오브젝트를 꺼냅니다. (Enum 사용)
        GameObject hitGo = Managers.Pool.Get<GameObject>(Define.Pool.NormalBullet_Hit);

        if (hitGo != null)
        {
            hitGo.transform.position = hitPos;
            float angle = Mathf.Atan2(hitNormal.y, hitNormal.x) * Mathf.Rad2Deg;
            hitGo.transform.rotation = Quaternion.Euler(0, 0, angle);
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

    // ★ [추가] 튕김 처리를 요청할 때 사용하는 함수
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
        _rb.linearVelocity = velocity;
    }

    #region ParticleSystem

    #endregion
}
