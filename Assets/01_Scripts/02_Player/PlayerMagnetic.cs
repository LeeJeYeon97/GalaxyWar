using UnityEngine;

public class PlayerMagnetic : MonoBehaviour
{
    private PlayerController _player;

    public void Init(PlayerController player)
    {
        //  부모 오브젝트에 있는 PlayerController를 미리 캐싱해 둡니다.
        _player = player;

        if (_player == null)
        {
            Debug.LogError("[PlayerItemCollector] 부모 오브젝트에서 PlayerController를 찾을 수 없습니다!");
        }

        UpdateMagneticRange();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Layer Matrix로 Item만 들어오게 세팅하셨겠지만, 안전하게 한 번 더 검사합니다.
        if (collision.TryGetComponent(out ItemController item))
        {
            // "아이템아, 연출 시작하고 내 쪽으로 날아와라!"
            item.TriggerCollection(_player);
        }
    }

    //  영구 강화나 인게임 레벨업으로 자석 범위가 늘어날 때 호출할 함수!
    public void UpdateMagneticRange()
    {
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = _player.Stat.itemGetRange.TotalValue;
        }
    }
}
