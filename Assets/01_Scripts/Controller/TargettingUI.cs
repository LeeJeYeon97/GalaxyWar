using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TargettingUI : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float _introDuration = 0.3f; // 처음에 크기가 작아지는 시간
    [SerializeField] private float _startScale = 2.5f;    // 시작 크기 (부모 대비)
    [SerializeField] private float _endScale = 1.5f;      // 락온 완료 후의 최종 크기
    [SerializeField] private float _blinkSpeed = 15f;     // 깜빡이는 속도

    private SpriteRenderer _spriteRenderer;
    private float _timer;
    private bool _isLocked = false;

    private Transform _target;
    private BulletController _bullet;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        // 처음엔 안 보이게
        _spriteRenderer.enabled = false;
    }

    // [핵심] 유도탄이 타겟을 잡았을 때 호출하는 함수
    public void Show(Transform targetTransform, BulletController bullet)
    {
        if (targetTransform == null)
            return;

        _target = targetTransform;
        _bullet = bullet;
        gameObject.SetActive(true);

        transform.SetParent(_target); // 타겟(메테오/보스)을 부모로 설정 (따라다니게)
        transform.localPosition = new Vector3(0, 0, -3f);
        transform.localRotation = Quaternion.identity; // 회전 초기화

        // 애니메이션 초기화
        _timer = 0;
        _isLocked = false;
        transform.localScale = Vector3.one * _startScale;

        // (중요) 이전 풀링에서 투명해진 상태로 꺼졌을 수 있으니 알파값을 1로 원상복구!
        Color color = _spriteRenderer.color;
        color.a = 1f;
        _spriteRenderer.color = color;

        _spriteRenderer.enabled = true;
    }

    // [핵심] 유도탄이 파괴되거나 타겟을 잃었을 때 호출
    public void Hide()
    {
        _spriteRenderer.enabled = false;
        _target = null;
        _bullet = null;

        transform.SetParent(null); // 풀로 돌아가기 전에 부모 연결을 끊어줍니다!

        Managers.Resource.Destroy(this.gameObject);
    }

    void Update()
    {
        //  [핵심 해결책 1] 자체 감시 시스템!
        // 총알이 풀로 돌아갔거나(죽음), 타겟이 풀로 돌아갔다면? -> 스스로 즉시 퇴근!
        if (_bullet == null || !_bullet.gameObject.activeInHierarchy ||
            _target == null || !_target.gameObject.activeInHierarchy)
        {
            Hide();
            return;
        }

        if (!_spriteRenderer.enabled) return;

        //  [핵심 해결책 2] 부모(메테오)가 아무리 덤블링을 해도, 내 회전은 무조건 정면(0,0,0)으로 고정!
        transform.rotation = Quaternion.identity;

        _timer += Time.deltaTime;

        if (!_isLocked)
        {
            // 1. 인트로 단계: 크기가 슈욱~ 하면서 작아짐
            float t = _timer / _introDuration;
            transform.localScale = Vector3.Lerp(Vector3.one * _startScale, Vector3.one * _endScale, t);

            if (t >= 1f)
            {
                _isLocked = true;
                // 완전히 고정됐을 때 소리
                // Managers.Sound.Play("Sound_Locked_Loop"); 
            }
        }
        else
        {
            // 2. 고정(Locked) 단계: 띠띠띠띠 하면서 미친듯이 깜빡거림 (Sin함수 활용)
            float alpha = (Mathf.Sin(Time.time * _blinkSpeed) + 1f) / 2f; // 0~1 반복

            // 0.2 ~ 1.0 사이로 알파값 조절 (너무 끄지 않게)
            Color color = _spriteRenderer.color;
            color.a = Mathf.Lerp(0.2f, 1.0f, alpha);
            _spriteRenderer.color = color;
        }
    }
}