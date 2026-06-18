using System.Collections;
using UnityEngine;

public class PoisonZoneController : MonoBehaviour
{
    [Header("Poison Settings")]
    private float _tickDamage;
    private float _radius;
    private float _duration;

    public float damageInterval = 0.5f;
    private float _timer = 0f;

    [Header("Visual Settings (Particle)")]
    // 잔상 대기 시간: 파티클 생성이 멈춘 후, 화면에 남은 연기들이 마저 사라질 때까지 기다리는 시간입니다.
    // 파티클의 'Start Lifetime' 최대값과 비슷하게 맞추면 정밀합니다.
    public float fadeOutDuration = 1.0f;
    private ParticleSystem _particleSystem;

    private void Awake()
    {
        // 파티클 컴포넌트 캐싱 
        // (만약 파티클이 자식 오브젝트에 들어있다면 GetComponentInChildren<ParticleSystem>()을 사용하세요)
        _particleSystem = GetComponentInChildren<ParticleSystem>();
    }

    public void Init(float damage, float radius, float duration)
    {
        _tickDamage = damage;
        _radius = radius;
        _duration = duration;
        _timer = 0f;

        // [풀링 방어] 재사용될 때 이전 판에서 날아가던 파티클 찌꺼기가 
        // 뿅 하고 순간 이동하듯 나타나는 현상을 완전히 지워버립니다.
        if (_particleSystem != null)
        {
            _particleSystem.Clear(); // 기존 파티클 싹 청소
            _particleSystem.Play();  // 새로 뿜어내기 시작
        }

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = _radius;
            col.enabled = true; // 콜라이더 재활성화
        }

        // [선택 스크립트] 코드에서 파티클 퍼지는 반경(Shape)을 동적으로 바꾸고 싶다면 사용하세요.
        if (_particleSystem != null)
        {
            var shape = _particleSystem.shape;
            shape.radius = _radius;
        }

        StartCoroutine(CoDestroyAfterDuration());
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _timer += Time.fixedDeltaTime;

            if (_timer >= damageInterval)
            {
                _timer = 0f;
                PlayerController player = collision.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    player.OnDamage(_tickDamage);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _timer = damageInterval;
        }
    }

    private IEnumerator CoDestroyAfterDuration()
    {
        // 1. 설정한 장판 지속 시간만큼 유지
        yield return new WaitForSeconds(_duration);

        // 2. 데미지 판정 콜라이더를 먼저 끕니다. (가스는 남아있지만 밟아도 안전한 상태)
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null) col.enabled = false;

        // 3.  파티클 생성 중단
        if (_particleSystem != null)
        {
            // StopEmitting 옵션을 주면 새 파티클은 안 나오지만, 이미 나온 연기들은 수명대로 살다 사라집니다.
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // 4. 맵에 남아있던 연기들이 완전히 투명해져서 안 보일 때까지 대기
        yield return new WaitForSeconds(fadeOutDuration);

        // 5. 모든 연출이 진짜 끝났으니 풀링 매니저로 오브젝트 반환
        Managers.Resource.Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        //Gizmos.color = new Color(0.5f, 1f, 0f, 0.3f);
        //Gizmos.DrawSphere(transform.position, GetComponent<CircleCollider2D>()?.radius ?? 1f);
    }
}