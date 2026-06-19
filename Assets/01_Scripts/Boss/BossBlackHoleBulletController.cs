using System.Collections;
using UnityEngine;

public class BossBlackHoleBulletController : MonoBehaviour
{
    [Header("블랙홀 설정")]
    public float lifeTime = 7f;         // 블랙홀이 맵에 존재하는 총 시간
    public float pullForce = 20f;       // 빨아들이는 힘
    public float centerDamage = 20f;     // 중심부 데미지
    public float damageInterval = 0.5f; // 데미지 간격
    public float travelDistance = 5f;   //  [추가] 멈추기 전까지 날아갈 거리

    private float _timer = 0f;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 startPos, Vector2 direction, float speed)
    {
        // 1. 기존에 돌고 있던 코루틴 찌꺼기가 있다면 확실하게 정지 (풀링 방어)
        StopAllCoroutines();

        // 2. 타이머 초기화 (이전에 쓰던 데미지 타이머 리셋)
        _timer = 0f;

        if (_rb != null)
        {
            // 3. transform.position 대신 Rigidbody의 position을 직접 옮겨야 
            // 물리 엔진이 순간이동을 즉시 알아채고 꼬이지 않습니다.
            _rb.position = startPos;

            // 4. 강제로 깨우기 (수면 상태 방지)
            _rb.WakeUp();
            _rb.linearVelocity = direction * speed;
        }
        else
        {
            // 만약 Rigidbody가 세팅되기 전이라면 임시로 transform 사용
            transform.position = startPos;
        }

        // 5. 콜라이더 강제 새로고침 (이전 충돌 캐시 지우기)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
            col.enabled = true;
        }

        // 6. 새로운 수명 및 정지 코루틴 시작
        StartCoroutine(CoProcessBlackHole(speed));
    }

    private IEnumerator CoProcessBlackHole(float speed)
    {
        // 1. 목표 거리까지 날아가는 데 걸리는 시간 계산 (시간 = 거리 / 속도)
        float travelTime = travelDistance / speed;

        // 예외 방어: 날아가는 시간이 전체 수명보다 길면 전체 수명으로 고정
        if (travelTime > lifeTime) travelTime = lifeTime;

        // 날아가는 시간만큼 이동 대기
        yield return new WaitForGameTime(travelTime);

        // 2. 목표 거리에 도달했으므로 속도를 즉시 0으로 만들어 그 자리에 고정!
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
        }

        // 3. 정지한 상태로 남은 시간 동안 중력장 유지 (전체 시간 - 날아간 시간)
        float remainTime = lifeTime - travelTime;
        if (remainTime > 0)
        {
            yield return new WaitForGameTime(remainTime);
        }

        // 4. 모든 시간이 끝났으므로 풀링 매니저로 반환
        Managers.Resource.Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Managers.Game.currentGameState != Define.GameState.Playing) return;

        if (collision.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.attachedRigidbody;
            if (playerRb != null)
            {
                Debug.Log("블랙홀이 끌어당기고있어요");
                // 플레이어를 블랙홀 중심점으로 끌어당김
                Vector2 pullDirection = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
                playerRb.AddForce(pullDirection * pullForce, ForceMode2D.Force);
            }

            // 중심부에 닿아있는 동안 주기적 데미지
            float distance = Vector2.Distance(transform.position, collision.transform.position);
            if (distance < 1.0f)
            {
                _timer += Time.fixedDeltaTime;
                if (_timer >= damageInterval)
                {
                    _timer = 0f;
                    Debug.Log("블랙홀 데미지");
                    PlayerController player = collision.GetComponentInParent<PlayerController>();
                    if (player != null) player.OnDamage(centerDamage);
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
}
