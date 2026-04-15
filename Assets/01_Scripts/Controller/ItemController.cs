using UnityEngine;

// 필드에 랜덤으로 드랍하는 아이템 컨트롤러
public class ItemController : MonoBehaviour
{

    private Rigidbody2D _rb;

    [SerializeField]
    private float minSpeed = 0.5f;
    private float maxSpeed = 1f;


    public bool _hasEnteredView;
    private float _checkOffset = 2.0f; // 경계 밖 여유 공간

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _hasEnteredView = false;
    }

    public void Init(Vector2 pos)
    {
        // 랜덤 데이터 설정
        ItemDataSO data = Managers.Data.ItemDataList[0];

        // 1.위치 설정
        transform.position = pos;

        // 플레이존의 랜덤한 곳으로 방향 계산
        float randX = Random.Range(Managers.Map.PlayZoneMin.x, Managers.Map.PlayZoneMax.x);
        float randY = Random.Range(Managers.Map.PlayZoneMin.y, Managers.Map.PlayZoneMax.y);
        Vector2 randPos = new Vector2(randX, randY);

        Vector2 dir = (randPos - pos).normalized;
        float speed = Random.Range(minSpeed, maxSpeed);
        _rb.linearVelocity = dir * speed;

        // 3. 랜덤한 회전 속도 부여 (초당 회전 각도)
        // -100 ~ 100 사이의 값을 주면 왼쪽 혹은 오른쪽으로 랜덤하게 돕니다.
        float randomTorque = Random.Range(-100f, 100f);
        _rb.angularVelocity = randomTorque;

    }
    private void Update()
    {
        CheckBoundaries();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Item 트리거");
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null) return;
    }
    void CheckBoundaries()
    {
        Vector3 pos = transform.position;
        var min = Managers.Map.PlayZoneMin;
        var max = Managers.Map.PlayZoneMax;

        // 1. 현재 화면 안인지 체크
        bool isInView = pos.x > min.x && pos.x < max.x && pos.y > min.y && pos.y < max.y;

        if (isInView)
        {
            _hasEnteredView = true;
        }

        // 2. 한 번 들어왔었는데, 다시 완전히 나갔다면 삭제
        if (_hasEnteredView)
        {
            if (pos.x < min.x - _checkOffset || pos.x > max.x + _checkOffset ||
                pos.y < min.y - _checkOffset || pos.y > max.y + _checkOffset)
            {
                Managers.Resource.Destroy(gameObject);
            }
        }
    }
}
