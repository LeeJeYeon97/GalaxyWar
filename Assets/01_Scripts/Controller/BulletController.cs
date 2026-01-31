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

    public int _currentHp = 0;
    public float _damage = 0;

    [SerializeField]
    private Vector2 _direction;


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

    public void SetBullet()
    {      
        // 불릿 데이터 뽑기
        _stat = Managers.Game.GetRandomBullet();

        _currentHp = (int)_stat.hp.TotalValue;

        _rb.angularVelocity = 0f;
    }


    // 충돌
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // "brick" 변수를 선언함과 동시에 컴포넌트가 있는지 시도함
        if (collision.gameObject.TryGetComponent<MeteorController>(out MeteorController brick))
        {
            _currentHp--;

            PlayHitEffect(collision);

            // 가지고 있는 능력 실행
            _stat.ability.Execute(_stat, brick);

            if (_currentHp <= 0)
            {
                // 반납처리 필요
                Managers.Pool.Release(gameObject);
            }
        }
       else if (collision.CompareTag("Wall"))
       {
           // 벽의 노멀값(Normal)을 가져오기 위해 레이캐스트를 살짝 쏩니다.
           RaycastHit2D hit = Physics2D.Raycast(transform.position, _direction, 0.5f, LayerMask.GetMask("Wall"));
       
           if (hit.collider != null)
           {
               // Vector2.Reflect(입사벡터, 법선벡터) = 반사벡터
               _direction = Vector2.Reflect(_direction, hit.normal);
               _rb.linearVelocity = _direction * _stat.speed.TotalValue;
       
               // 총알의 회전도 반사 방향에 맞춰 업데이트 (선택 사항)
               float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
               transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
           }
       }
    }
    
    // 발사
    public void Shot(Vector2 dragVector)
    {
        _direction = dragVector.normalized;
        Vector2 force = dragVector * _stat.speed.TotalValue;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(force, ForceMode2D.Impulse);

        // --- 추가: 발사 방향으로 총알 회전 ---
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        // 파티클이 기본적으로 위(Y축)를 향해 뿜어져 나온다면 -90을 해줍니다.
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

    }
    
    // 발사대에 있을 때 드래그중이면 Kinematic으로
    public void SetPhysicsState(bool isKinematic)
    {
        _rb.bodyType = isKinematic ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        if (isKinematic) _rb.linearVelocity = Vector2.zero;
    }
    // 히트 이펙트를 재생하는 별도 함수
    private void PlayHitEffect(Collider2D collision)
    {
        // 1. 풀에서 이펙트 오브젝트를 꺼냅니다. (Enum 사용)
        GameObject hitGo = Managers.Pool.Get<GameObject>(Define.Pool.NormalBullet_Hit);

        if (hitGo != null)
        {
            // 2. 충돌 위치 결정
            // OnTrigger는 정확한 ContactPoint를 제공하지 않으므로, 
            // 가장 가까운 지점을 찾거나 총알의 현재 위치를 사용합니다.
            Vector3 hitPos = collision.ClosestPoint(transform.position);
            hitGo.transform.position = hitPos;

            // 3. 만약 이펙트 프리팹에 ParticleAutoRelease(아까 만든 것)가 붙어있다면
            // 여기서 별도로 꺼줄 필요 없이 지가 알아서 재생하고 풀로 돌아갑니다.
        }
    }

    #region ParticleSystem

    #endregion
}
