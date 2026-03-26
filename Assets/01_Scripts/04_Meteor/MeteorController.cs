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

    private Coroutine _freezeCoroutine;
    private Coroutine _slowCoroutine;
    
    // 슬로우를 위한 현재 속도 및 방향 저장
    public Stat currentSpeed = new Stat();
    private Vector2 _moveDir;
    private float _baseAngularVelocity; // 원래 팽이처럼 돌던 회전력

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

        // 상태 초기화
        _hasEnteredView = false;
        _hasAuraBuff = false;
        _isPaused = false;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.white;
            _originalColor = _spriteRenderer.color; // 내 원래 색상 기억
        }

        // 1.위치 설정
        transform.position = pos;

        // 2. 플레이어 방향으로 방향 계산
        _moveDir = ((Vector2)Managers.Game._player.transform.position - pos).normalized;

        // 3. 랜덤으로 속도 뽑기
        currentSpeed.Init(Random.Range(Stat.MinSpeed.TotalValue, Stat.MaxSpeed.TotalValue));

        // 5. 랜덤한 회전 속도 부여 (초당 회전 각도)
        // -100 ~ 100 사이의 값을 주면 왼쪽 혹은 오른쪽으로 랜덤하게 돕니다.
        _baseAngularVelocity = Random.Range(-100f, 100f);
        _rb.simulated = true;
        UpdateVelocity();

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
    private void UpdateVelocity()
    {
        // 내 고유 스탯(currentSpeed)의 '최종 계산된 값(TotalValue)'을 바로 방향에 곱해줍니다.
        _rb.linearVelocity = _moveDir * currentSpeed.TotalValue;

        // 회전력 처리: 완전 빙결(TotalValue == 0)일 때는 멈추고, 아니면 돌게 만듭니다.
        if (currentSpeed.TotalValue == 0)
            _rb.angularVelocity = 0f;
        else
            _rb.angularVelocity = _baseAngularVelocity; // (원한다면 슬로우 비율만큼 곱해줘도 됩니다!)
    }
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
    //물리 엔진 복구 로직
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


    public void ApplySlow(float slowPercent, float duration)
    {
        if (!gameObject.activeInHierarchy) return;

        // 이미 슬로우가 걸려있다면 기존 코루틴을 끄고 시간을 리셋!
        if (_slowCoroutine != null)
        {
            StopCoroutine(_slowCoroutine);
            // 주의: 코루틴을 강제로 끄기 전에 깎였던 스탯을 한 번 원상복구 해줘야 중첩 버그가 안 생깁니다!
            currentSpeed.SubMultiplier(-slowPercent);
            UpdateVelocity();
        }

        _slowCoroutine = StartCoroutine(CoSlowRoutine(slowPercent, duration));
    }

    // 슬로우 루틴
    private IEnumerator CoSlowRoutine(float slowPercent, float duration)
    {
        // 1. 스탯 깎기 (예: 50% 슬로우면 multiplier에 -0.5를 더함)
        currentSpeed.AddMultiplier(-slowPercent);

        // 속도에 반영
        UpdateVelocity();

        // 시각적 효과 (파랗게 질림)
        if (_spriteRenderer != null) _spriteRenderer.color = new Color(0.5f, 0.8f, 1f);

        // 2. 지정된 시간만큼 대기
        yield return new WaitForSeconds(duration);

        // 3. 스탯 완벽하게 원상 복구! (깎았던 만큼 다시 빼줌)
        currentSpeed.SubMultiplier(-slowPercent);

        // 시각적 효과 복구
        if (_spriteRenderer != null && !_hasAuraBuff) _spriteRenderer.color = _originalColor;

        _slowCoroutine = null;
    }

    // 완전 빙결(Freeze)도 똑같은 방식으로 창구를 열어줍니다.
    public void ApplyFreeze(float duration)
    {
        // 1. 이미 얼어있는 상태에서 또 맞았다면? -> 빙결 시간 리셋!
        if (_freezeCoroutine != null)
        {
            StopCoroutine(_freezeCoroutine);
            // 코루틴을 강제로 끄기 전에 스탯을 한 번 원상복구 해줘야 버그가 안 생깁니다.
            currentSpeed.SetForceZero(false);
            UpdateVelocity();
        }
        _freezeCoroutine = StartCoroutine(CoFreezeRoutine(duration));
    }
    private IEnumerator CoFreezeRoutine(float duration)
    {
        // 1. 스탯 강제 0 스위치 ON! (유저님의 Stat 클래스 기능 활용)
        currentSpeed.SetForceZero(true);

        // 2. 물리 엔진(Rigidbody) 완전히 멈추기
        UpdateVelocity();
        

        // 시각적 효과 (슬로우보다 조금 더 쨍하고 진한 얼음색!)
        if (_spriteRenderer != null)    _spriteRenderer.color = new Color(0.2f, 0.6f, 1f);

        // 3. 빙결 지속 시간(예: 2초) 동안 대기
        yield return new WaitForSeconds(duration);

        // 4. 해동! 스탯 강제 0 스위치 OFF!
        currentSpeed.SetForceZero(false);
        UpdateVelocity(); // -> 0 스위치가 풀렸으니 원래 속도(슬로우가 걸려있다면 깎인 속도)로 튀어나갑니다!

        if (_spriteRenderer != null && !_hasAuraBuff) _spriteRenderer.color = _originalColor;

        _freezeCoroutine = null;
    }
}
