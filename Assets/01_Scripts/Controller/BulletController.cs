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
    // 바뀌는 값은 여기서 선언
    public bool canSplit { get; private set; }
    public float currentBounceCount { get; private set; }

    #region Particle변수

    #endregion

    public void Awake()
    {
        if(_rb == null) _rb = Util.GetOrAddComponent<Rigidbody2D>(gameObject);
        
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
        currentBounceCount = stat.bounceCount.TotalValue;
        canSplit = true;

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
        PlayHitEffect(collision);

        currentBounceCount--;
        // "brick" 변수를 선언함과 동시에 컴포넌트가 있는지 시도함
        MeteorController meteor = collision.gameObject.GetComponentInParent<MeteorController>();
        if (meteor)
        {
            // 파라미터 팩 만들기
            AbilityExecuteParams param = new AbilityExecuteParams
            {
                stat = _stat,
                bullet = this,
                target = meteor,
                collision = collision, // 충돌 정보 통째로 전달
                incomingDirection = _rb.linearVelocity.normalized // 들어온 방향
            };

            // 가지고 있는 능력 실행
            _stat.ability.Execute(param);   
        }
        if (currentBounceCount < 0)
        {
            // 반납처리 필요
            Managers.Pool.Release(gameObject);
        }
    }
    
    // 발사
    public void Shot(Vector2 dragVector)
    {
        if (gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }
        Vector2 force = dragVector * _stat.speed.TotalValue;

        SetPhysicsState(false);
        _rb.AddForce(force, ForceMode2D.Impulse);

    }
    
    // 발사대에 있을 때 드래그중이면 Kinematic으로
    public void SetPhysicsState(bool isKinematic)
    {
        _rb.bodyType = isKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        if (isKinematic) _rb.linearVelocity = Vector2.zero;
    }
    // 히트 이펙트를 재생하는 별도 함수
    private void PlayHitEffect(Collision2D collision)
    {
        // 1. 풀에서 이펙트 오브젝트를 꺼냅니다. (Enum 사용)
        GameObject hitGo = Managers.Pool.Get<GameObject>(Define.Pool.NormalBullet_Hit);

        if (hitGo != null)
        {
            // 2. 충돌 위치 결정
            // OnTrigger는 정확한 ContactPoint를 제공하지 않으므로, 
            // 가장 가까운 지점을 찾거나 총알의 현재 위치를 사용합니다.
            Vector3 hitPos = collision.contacts[0].point;
            hitGo.transform.position = hitPos;

            // : 이펙트가 튕겨나가는 방향(Normal)을 바라보게 하고 싶다면?
            Vector2 hitNormal = collision.contacts[0].normal;
            float angle = Mathf.Atan2(hitNormal.y, hitNormal.x) * Mathf.Rad2Deg;
            hitGo.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    #region ParticleSystem

    #endregion
}
