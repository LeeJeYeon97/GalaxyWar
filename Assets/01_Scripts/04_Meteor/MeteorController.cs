using DG.Tweening;
using System.Collections;
using TMPro;
using Unity.AppUI.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class MeteorController : MonoBehaviour
{
    [SerializeField] 
    private Image _hpBar;
    
    [SerializeField]
    private float _currentHp;

    [SerializeField]
    private float _maxHp;
    public MeteorStat Stat;

    public Rigidbody2D _rb;

    public bool _hasEnteredView;
    private float _checkOffset = 2.0f; // 경계 밖 여유 공간
    private SpriteRenderer _spriteRenderer; // 색상을 바꾸기 위해 렌더러 캐싱

    //추가: 일시정지 상태일 때 속도와 회전력을 기억해둘 변수
    private Vector2 _savedVelocity;
    private float _savedAngularVelocity;
    private bool _isPaused = false;

    private bool _hasAuraBuff = false;      // 내가 지금 오라 버프를 받고 있는가?
    private Color _originalColor;           // 내 원래 색상을 기억할 변수
    
    public Coroutine ActionCoroutine;
    private float _auraBuffEndTime = 0f; // 코루틴 대신 종료 시간을 기억할 변수!
    private void Awake()
    {
        _hpBar = Util.FindChild<Image>(gameObject, "HpBar", true);
        _rb = Util.GetOrAddComponent<Rigidbody2D>(gameObject);
        _spriteRenderer = GetComponent<SpriteRenderer>();

        _hasEnteredView = false;
    }
    public void Init(Vector2 pos, MeteorStat stat)
    {
        if (stat == null)
        {
            return;
        }
        
        Stat = stat;
        _maxHp = Stat.MaxHp.TotalValue;
        _currentHp = _maxHp;

        // 1.위치 설정
        transform.position = pos;

        // 상태 초기화
        _hasEnteredView = false;
        _hasAuraBuff = false;
        _isPaused = false;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.white;
            _originalColor = _spriteRenderer.color; // 내 원래 색상 기억
        }

        // 플레이어 방향으로 방향 계산
        Vector2 dir = ((Vector2)Managers.Game._player.transform.position - pos).normalized;
        float speed = Random.Range(Stat.MinSpeed.TotalValue, Stat.MaxSpeed.TotalValue);
        _rb.linearVelocity = dir * speed;

        // 3. 랜덤한 회전 속도 부여 (초당 회전 각도)
        // -100 ~ 100 사이의 값을 주면 왼쪽 혹은 오른쪽으로 랜덤하게 돕니다.
        float randomTorque = Random.Range(-100f, 100f);
        _rb.angularVelocity = randomTorque;
        _rb.simulated = true;


        Stat.Behavior?.OnInit(this);

        UpdateHPBar();
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player == null) return;

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
        if (Stat != null)
        {
            Stat.Behavior?.OnRelease(this);
        }
        Managers.Game.RemoveActiveObject(this);
    }
    private void Update()
    {
        //게임 상태가 'Playing'이 아닐 때 (일시정지, 게임오버 등)
        if (Managers.Game.currentGameState != Define.GameState.Playing)
        {
            if (!_isPaused)
            {
                PausePhysics(); // 멈춰!
            }
            return; // 멈춰있는 동안에는 아래의 CheckBoundaries() 등도 실행 안 함
        }

        //게임 상태가 'Playing'으로 돌아왔을 때
        if (_isPaused)
        {
            ResumePhysics();
        }

        // 버프 만료 체크 (Update에서 가볍게 시간만 비교!)
        if (_hasAuraBuff && Time.time > _auraBuffEndTime)
        {
            LoseAuraBuff();
        }

        Stat.Behavior?.OnUpdate(this);
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

            // 핵심: Stat 원본을 수정하지 않고, 들어온 damage 변수값만 즉석에서 반토막 냅니다!
            if (_hasAuraBuff)
            {
                damage *= 0.5f; // 오라를 받고 있다면 데미지 50% 감소
            }
            GameObject go = Managers.Resource.Instantiate("DamageText");
            DamageText damageText = go.GetOrAddComponent<DamageText>();
            if (damageText != null)
            {
                damageText.Init(textPos, Mathf.FloorToInt(damage));
            }
            
            if (_currentHp <= 0)
            {
                Die();
            }
            else
            {
                UpdateHPBar();
            }
        }
    }
    private void Die()
    {
        Stat.Behavior?.OnDie(this);
        Managers.Level.AddExp(Stat.Exp.TotalValue);
        Managers.Pool.Release(gameObject);
    }

    #region 물리 연산
    private void PausePhysics()
    {
        _isPaused = true;

        // 현재 날아가던 속도와 팽이처럼 돌던 회전값을 변수에 저장
        _savedVelocity = _rb.linearVelocity;
        _savedAngularVelocity = _rb.angularVelocity;

        // 속도 0으로 강제 고정하고, 다른 물체랑 부딪혀서 밀려나지 않게 물리 시뮬레이션을 끕니다.
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        _rb.simulated = false; // 충돌 및 물리 연산 완전 정지
    }
    //=물리 엔진 복구 로직
    private void ResumePhysics()
    {
        _isPaused = false;

        // 물리 연산을 다시 켜고, 아까 저장해뒀던 속도를 그대로 다시 주입
        _rb.simulated = true;
        _rb.linearVelocity = _savedVelocity;
        _rb.angularVelocity = _savedAngularVelocity;
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
    #endregion


    // 다른 일반 운석들이 오라 버프를 받을 때 실행되는 함수
    public void ReceiveAuraBuff(float duration)
    {
        // 1. 버프 종료 시간을 "현재 시간 + 0.3초"로 연장(리필)합니다.
        _auraBuffEndTime = Time.time + duration;

        // 2. 처음 버프를 받은 거라면 색깔을 노랗게 바꿔줍니다.
        if (!_hasAuraBuff)
        {
            _hasAuraBuff = true;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.yellow;
            }
        }
    }
    private void LoseAuraBuff()
    {
        _hasAuraBuff = false;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _originalColor;
        }
    }
    private void OnDrawGizmos()
    {
        // 아직 런타임이 아니라 Stat이 없거나, 오라 운석이 아닐 때는 그리지 않음
        if (Stat == null || Stat.type != MeteorType.AuraBuffMeteor) return;

        // 노란색의 반투명한 선으로 반경(auraRadius)을 그립니다.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Stat.auraRadius.TotalValue);
    }
}
