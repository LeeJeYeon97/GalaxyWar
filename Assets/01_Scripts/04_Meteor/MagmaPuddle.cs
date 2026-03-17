using System.Collections;
using UnityEngine;

public class MagmaPuddle : MonoBehaviour
{
    public float damage = 5f;        // 장판 틱 데미지
    public float lifeTime = 3f;      // 장판 유지 시간
    public float damageTick = 0.5f;  // 0.5초마다 데미지를 줌

    private float _lastDamageTime;

    public void Init(Vector2 pos, float magmaDamage)
    {
        transform.position = pos;
        damage = magmaDamage;
        _lastDamageTime = 0f;

        // 3초 뒤에 풀로 돌아가도록 코루틴 실행
        StartCoroutine(CoDestroySelf());
    }

    private IEnumerator CoDestroySelf()
    {
        yield return new WaitForSeconds(lifeTime);
        Managers.Pool.Release(gameObject);
    }

    // 플레이어가 장판 위에 머무는 동안 데미지 주기
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (Managers.Game.currentGameState != Define.GameState.Playing) return;

        if (collision.CompareTag("Player"))
        {
            // 연속으로 데미지가 들어가는 것을 막기 위한 쿨타임 체크
            if (Time.time - _lastDamageTime >= damageTick)
            {
                _lastDamageTime = Time.time;
                PlayerController player = collision.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.OnDamage(damage);
                }
            }
        }
    }
}