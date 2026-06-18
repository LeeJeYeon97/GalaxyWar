using System.Collections;
using UnityEngine;

public class SludgePuddle : MonoBehaviour
{
    public float lifeTime = 3f;           // 장판 유지 시간 (3초)
    public float slowMultiplier = 0.5f;   // 이동 속도 50%로 감소

    [Header("Visual Settings (Particle)")]
    // 잔상 대기 시간: 파티클 생성이 멈춘 후 남은 연기가 사라질 때까지의 시간
    public float fadeOutDuration = 1.0f;
    private ParticleSystem _particleSystem;
    private Collider2D _collider; // 2D 콜라이더 캐싱용

    private void Awake()
    {
        // 파티클과 콜라이더 컴포넌트를 미리 캐싱해둡니다.
        _particleSystem = GetComponentInChildren<ParticleSystem>();
        _collider = GetComponent<Collider2D>();
    }

    public void Init(Vector2 pos)
    {
        transform.position = pos;

        // [풀링 방어] 재사용 시 이전 파티클 찌꺼기 청소 및 재생
        if (_particleSystem != null)
        {
            _particleSystem.Clear();
            _particleSystem.Play();
        }

        // [풀링 방어] 삭제될 때 꺼졌던 콜라이더를 다시 켜줍니다.
        if (_collider != null)
        {
            _collider.enabled = true;
        }

        StartCoroutine(CoDestroySelf());
    }

    private IEnumerator CoDestroySelf()
    {
        // 1. 설정한 장판 지속 시간(lifeTime)만큼 유지
        yield return new WaitForGameTime(lifeTime);

        // 2. 디버프 판정 콜라이더를 끕니다. (밟아도 느려지지 않는 안전 상태)
        if (_collider != null)
        {
            _collider.enabled = false;
        }

        // 3. 파티클 생성 중단 (새 연기는 안 나오고, 남은 연기만 서서히 사라짐)
        if (_particleSystem != null)
        {
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // 4. 잔상이 완전히 사라질 때까지 대기
        yield return new WaitForGameTime(fadeOutDuration);

        // 5. 연출이 다 끝났으니 풀링 매니저로 반환
        Managers.Resource.Destroy(gameObject);
    }

    // 장판 위에 머무는 동안 계속 실행됨
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Managers.Game.currentGameState != Define.GameState.Playing) return;

        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                // 플레이어에게 "0.2초 동안 속도를 50%로 깎아라!" 라고 명령
                Managers.Stat.ApplyPlayerDebuff(Define.DebuffType.Slow, slowMultiplier, 0.2f);
            }
        }
    }
}