using System.Collections;
using UnityEngine;

public class SludgePuddle : MonoBehaviour
{
    public float lifeTime = 3f;           // 장판 유지 시간 (3초)
    public float slowMultiplier = 0.5f;   // 이동 속도 50%로 감소

    public void Init(Vector2 pos)
    {
        transform.position = pos;
        StartCoroutine(CoDestroySelf());
    }

    private IEnumerator CoDestroySelf()
    {
        yield return new WaitForGameTime(lifeTime);
        Managers.Resource.Destroy(gameObject); // 3초 뒤 스스로 풀로 돌아감
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
