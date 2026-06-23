using UnityEngine;

public class MeteorFire : MonoBehaviour
{
    [Header("불기둥 설정")]
    public float tickDamage = 5f;       // 불기둥 초당(틱) 데미지
    public float damageInterval = 0.5f; // 데미지가 들어가는 간격

    private float _timer = 0f;

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 주의: 이 불기둥 자체에도 Rigidbody2D가 있으면 안 됩니다. 
        // 오직 Collider2D(IsTrigger=true)만 있어야 합니다.

        if (collision.CompareTag("Player"))
        {
            _timer += Time.fixedDeltaTime;

            if (_timer >= damageInterval)
            {
                _timer = 0f; // 타이머 초기화

                PlayerController player = collision.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    Debug.Log("불기둥 맞음");
                    // 불기둥 전용 데미지 적용 (밀어내기 없음!)
                    player.OnDamage(tickDamage,false, this.gameObject);
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // 플레이어가 밖으로 나가면 즉시 데미지를 줄 수 있도록 타이머 리셋
        if (collision.CompareTag("Player"))
        {
            _timer = damageInterval;
        }
    }
}
