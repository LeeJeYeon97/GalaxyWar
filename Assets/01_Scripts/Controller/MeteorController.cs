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
    public MeteorStat Stat;

    private Rigidbody2D _rb;

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
        // 랜덤으로 스탯뽑기
        Stat = Managers.Stat.GetRandomMeteorStat();
        if (Stat == null) return;

        // 1.위치 설정
        transform.position = pos;
        _hasEnteredView = false;

        // 2. 랜덤 스케일 설정
        //float rawRandom = Random.Range(minScale.x, maxScale.x);
        //float snappedScale = Mathf.Round(rawRandom * 100f) / 100f;
        //transform.localScale = new Vector3(snappedScale, snappedScale, 1f);

       
        // 플레이어 방향으로 방향 계산
        Vector2 dir = ((Vector2)Managers.Game._player.transform.position - pos).normalized;
        float speed = Random.Range(Stat.MinSpeed.TotalValue, Stat.MaxSpeed.TotalValue);
        _rb.linearVelocity = dir * speed;

        // 3. 랜덤한 회전 속도 부여 (초당 회전 각도)
        // -100 ~ 100 사이의 값을 주면 왼쪽 혹은 오른쪽으로 랜덤하게 돕니다.
        float randomTorque = Random.Range(-100f, 100f);
        _rb.angularVelocity = randomTorque;

        _maxHp = Stat.MaxHp.TotalValue;
        _currentHp = _maxHp;
        UpdateHPBar();
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("메테오 피격");
        // 플레이어 피격 설정
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player == null) return;

            Debug.Log("플레이어 피격");
            player.OnDamage(Stat.Damage.TotalValue);
        }
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
            Managers.Level.AddScore(Mathf.FloorToInt(Stat.Score.TotalValue));

            Vector3 textPos = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 0.5f, 0);

            DamageText damageText = Managers.Pool.Get<DamageText>(Define.Pool.DamageText);
            if (damageText != null)
            {
                damageText.Init(textPos, Mathf.FloorToInt(damage));
            }
            if(damageText == null)
            {
                Debug.Log("데미지 텍스트 없음");
            }

            if (_currentHp <= 0)
            {
                Managers.Level.AddExp(Stat.Exp.TotalValue);
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
