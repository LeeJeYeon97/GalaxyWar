using DG.Tweening;
using TMPro;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEngine.UI;

public class MeteorController : MonoBehaviour
{
    [SerializeField] 
    private Image _hpBar;
    
    [SerializeField]
    private float _currentHp;
    [SerializeField]
    private float _maxHp;
    
    private Vector3 minScale = new Vector3(0.1f, 0.1f, 1f);
    private Vector3 maxScale = new Vector3(0.3f, 0.3f, 1f);

    private Rigidbody2D _rb;
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float maxSpeed = 1f;

    public bool _hasEnteredView;
    private float _checkOffset = 2.0f; // 경계 밖 여유 공간

    private void Awake()
    {
        _hpBar = Util.FindChild<Image>(gameObject, "HpBar", true);
        _rb = Util.GetOrAddComponent<Rigidbody2D>(gameObject);
        
        // 우주이므로 중력은 0이어야 함
        _rb.gravityScale = 0;
        _hasEnteredView = false;
    }
    public void Init(Vector2 pos)
    {

        // 1.위치 설정
        transform.position = pos;
        _hasEnteredView = false;

        // 2. 랜덤 스케일 설정
        float rawRandom = Random.Range(minScale.x, maxScale.x);
        float snappedScale = Mathf.Round(rawRandom * 100f) / 100f;
        transform.localScale = new Vector3(snappedScale, snappedScale, 1f);

       
        // 플레이어 방향으로 방향 계산
        Vector2 dir = ((Vector2)Managers.Game._player.transform.position - pos).normalized;
        float speed = Random.Range(minSpeed, maxSpeed);
        _rb.linearVelocity = dir * speed;

        // 3. 랜덤한 회전 속도 부여 (초당 회전 각도)
        // -100 ~ 100 사이의 값을 주면 왼쪽 혹은 오른쪽으로 랜덤하게 돕니다.
        float randomTorque = Random.Range(-100f, 100f);
        _rb.angularVelocity = randomTorque;

        _maxHp = 1;
        _currentHp = _maxHp;
        UpdateHPBar();
    }
    private void UpdateHPBar()
    {
        if (_hpBar == null)
            return;

        // 현재 체력 비율 (0.0 ~ 1.0)
        float ratio = _currentHp / _maxHp;

        // DOFillAmount(목표값, 시간) 사용
        _hpBar.DOKill(); // 이전 애니메이션이 실행 중이면 중지
        _hpBar.DOFillAmount(ratio, 0.2f).SetEase(Ease.OutCubic);
    }
    private void OnEnable()
    {
        Managers.Game.AddActiveObject(this);
    }
    private void OnDisable()
    {
        Managers.Game.RemoveActiveObject(this);
    }
    private void Update()
    {
        CheckBoundaries();
    }
    public void OnDamage(float damage)
    {
        if(damage > 0)
        {
            _currentHp -= damage;

            // 때릴때마다 점수 1점
            Managers.Game.AddScore(1);

            if (_currentHp <= 0)
            {
                Managers.Level.AddExp(1);
                Managers.Pool.Release(gameObject);
            }
            else
            {
                UpdateHPBar();
            }
        }
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
                Managers.Pool.Release(gameObject);
            }
        }
    }
    
}
