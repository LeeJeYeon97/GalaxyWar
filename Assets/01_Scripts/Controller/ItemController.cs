using DG.Tweening;
using System.Collections;
using UnityEngine;

public class ItemController : MonoBehaviour
{

    private Rigidbody2D _rb;
    private Collider2D _collider;
    ItemAnimation _anim;

    [SerializeField]
    private float minSpeed = 0.5f;
    private float maxSpeed = 1f;


    public bool _hasEnteredView;
    private float _checkOffset = 2.0f; // 경계 밖 여유 공간

    private ItemDataSO _data;
    public int value;

    // 획득 연출 중복 실행 방지용 플래그
    private bool _isCollecting = false;

    private Vector2 myScale;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _anim = GetComponent<ItemAnimation>();
        myScale = transform.localScale;
    }

    public void Init(Vector2 pos, ItemDataSO data, int customValue = 0)
    {
        // 데이터 설정
        if (data == null) return;

        _data = data;
        // 1.위치 설정
        transform.position = pos;

        _hasEnteredView = false;

        // ==========================================================
        // [추가] 오브젝트 풀링을 위해 껐던 것들 완벽하게 초기화 (원상복구)
        // ==========================================================
        _isCollecting = false;
        _rb.simulated = true;
        _collider.enabled = true;
        transform.localScale = myScale; // 두트윈으로 바뀔 크기 초기화

        // 끄고 놔뒀던 통통 튀기 애니메이션 다시 켜기

        //통통 튀기 스크립트 끄기!
        if (_anim != null)
        {
            _anim.enabled = true;
            _anim.SetStartPosition(pos); // "이제부터 네 고향은 여기(pos)야!"
        }
        if (customValue != 0)
        {
            value = customValue;
        }
        else
        {
            value = Random.Range(data.minValue, data.maxValue);
        }

        // 메테오가 떨구는게 아닌 맵에 랜덤으로 스폰되는 아이템의 경우
        if (data.isDrop == false)
        {
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
    }
    private void Update()
    {
        // 획득 연출 중에는 경계 체크를 하지 않습니다.
        if (!_isCollecting)
        {
            CheckBoundaries();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //  이미 획득 연출이 시작되었다면 무시
        if (_isCollecting) return;

        // 아이템 획득// 수정된 코드 (콜라이더가 리지드바디에 붙어있지 않을 수도 있으므로 ? 연산자 사용)
        //콜라이더를 지배하고 있는 리지드바디(부모)로 다이렉트로 접근해서 스크립트를 가져옵니다.
        //PlayerController player = collision.GetComponentInParent<PlayerController>();
        PlayerController player = collision.attachedRigidbody?.GetComponent<PlayerController>();
        if (player == null) return;

        //  1. 획득 시작: 물리 연산 끄기 및 콜라이더 비활성화
        _isCollecting = true;

        _rb.simulated = false;

        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        _collider.enabled = false; // 중복 충돌 완벽 방지

        //통통 튀기 스크립트 끄기!
        if (_anim != null)
        {
            _anim.enabled = false; // "이제 그만 통통 튀고 플레이어한테 끌려가!"
        }
        //  2. 연출 코루틴 시작//
        //  DOTween 쫀득 연출 시작!
        DoCollectTween(player);

    }

    // ==========================================================
    //  DOTween 쫀득 애니메이션 로직
    // ==========================================================
    private void DoCollectTween(PlayerController player)
    {
        Vector2 startPos = transform.position;
        Vector2 bounceDir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
        Vector2 bounceTarget = startPos + bounceDir * 1.2f; // 뒤로 밀려날 목표 지점

        // DOTween 시퀀스 생성 (연속 동작)
        Sequence seq = DOTween.Sequence();

        // [동작 1] 뒤로 튕겨나가기 (OutCubic: 처음에 팍! 밀려나고 끝에서 부드럽게 감속)
        seq.Append(transform.DOMove(bounceTarget, 0.25f).SetEase(Ease.OutCubic));

        // [동작 2] 튕겨나갈 때 크기가 1.5배로 '팝(Pop)' 하고 커지기 (Join은 앞 동작과 동시에 실행)
        seq.Join(transform.DOScale(myScale * 1.3f, 0.15f).SetEase(Ease.OutQuad));

        // [동작 3] 커졌던 크기를 다시 원래대로 쫀득하게 줄이기
        seq.Append(transform.DOScale(myScale, 0.1f).SetEase(Ease.InQuad));

        // 튕기는 연출이 모두 끝나면? -> 맹추격 코루틴 시작!
        seq.OnComplete(() =>
        {
            StartCoroutine(CoCollectAnimation(player));
        });
    }

    private IEnumerator CoCollectAnimation(PlayerController player)
    {
        //// --- [페이즈 1] 뒤로 튕기기 (Vector2 유지) ---
        //Vector2 startPos = transform.position;
        //Vector2 bounceDir = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;

        //float bounceDuration = 0.2f;
        //float bounceDistance = 1.0f;
        //Vector2 bounceTarget = startPos + bounceDir * bounceDistance;

        //float time = 0;
        //while (time < bounceDuration)
        //{
        //    time += Time.deltaTime;
        //    float t = time / bounceDuration;
        //    transform.position = Vector2.Lerp(startPos, bounceTarget, Mathf.Sin(t * Mathf.PI * 0.5f));
        //    yield return null;
        //}

        float chaseSpeed = 0f;    // 잠깐 멈칫! 했다가 출발하는 느낌을 위해 0부터 시작
        float acceleration = 40f; // 엄청난 가속도로 빨려 들어감

        while (player != null && player.gameObject.activeSelf)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            // 2D 픽셀 게임에 맞게 도달 판정 거리를 다시 0.5f로 좁혔습니다!
            if (distance < 0.5f)
            {
                break;
            }
            chaseSpeed += acceleration * Time.deltaTime;

            // 순수 Vector2.MoveTowards 사용
            transform.position = Vector2.MoveTowards(transform.position, player.transform.position, chaseSpeed * Time.deltaTime);
            yield return null;
        }

        // --- [페이즈 3] 획득 및 파괴 ---
        switch (_data.type)
        {
            case Define.ItemType.Gold:
                Managers.Game.currentSessionGold += value;
                Managers.Event.PostEvent(Define.ActionEvent.GetGold);
                break;
            case Define.ItemType.Exp:
                Managers.Level.AddExp(value);
                break;
        }

        Managers.Sound.Play(_data.getSoundClip);
        Managers.Resource.Destroy(this.gameObject);
    }
    void CheckBoundaries()
    {
        if (_data.isDrop == false) return;

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
