using DG.Tweening;
using System.Collections;
using TMPro;
using Unity.AppUI.Core;
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

    private Rigidbody2D _rb;

    public bool _hasEnteredView;
    private float _checkOffset = 2.0f; // 경계 밖 여유 공간

    //추가: 일시정지 상태일 때 속도와 회전력을 기억해둘 변수
    private Vector2 _savedVelocity;
    private float _savedAngularVelocity;
    private bool _isPaused = false;


    private Coroutine _magmaCoroutine;

    private void Awake()
    {
        _hpBar = Util.FindChild<Image>(gameObject, "HpBar", true);
        _rb = Util.GetOrAddComponent<Rigidbody2D>(gameObject);
        
        // 우주이므로 중력은 0이어야 함
        _rb.gravityScale = 0;
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
        _hasEnteredView = false;

        // 초기화할 때는 당연히 정지 상태가 아님
        _isPaused = false;

        // 플레이어 방향으로 방향 계산
        Vector2 dir = ((Vector2)Managers.Game._player.transform.position - pos).normalized;
        float speed = Random.Range(Stat.MinSpeed.TotalValue, Stat.MaxSpeed.TotalValue);
        _rb.linearVelocity = dir * speed;

        // 3. 랜덤한 회전 속도 부여 (초당 회전 각도)
        // -100 ~ 100 사이의 값을 주면 왼쪽 혹은 오른쪽으로 랜덤하게 돕니다.
        float randomTorque = Random.Range(-100f, 100f);
        _rb.angularVelocity = randomTorque;
        _rb.simulated = true;

        UpdateHPBar();
        InitType();
    }
    // 메테오 타입에 따라서 추가로 해줘야 하는 로직
    private void InitType()
    {
        switch(Stat.Type)
        {
            case MeteorType.NormalMeteor:
                break;
            case MeteorType.CometMeteor:
                break;
            case MeteorType.IronMeteor:
                break;
            case MeteorType.FractureMeteor:
                break;
            case MeteorType.Fragment:
                Debug.Log("파편이 뽑혔어요");
                Vector2 scatterDir = Random.insideUnitCircle.normalized;
                float speed = Random.Range(Stat.MinSpeed.TotalValue, Stat.MaxSpeed.TotalValue);
                _rb.linearVelocity = scatterDir * speed;
                break;
            case MeteorType.MagmaMeteor:
                Debug.Log("마그마가 뽑혔어요");
                if (_magmaCoroutine != null) StopCoroutine(_magmaCoroutine);
                _magmaCoroutine = StartCoroutine(CoDropMagma());
                break;
        }
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

        // 추가: 운석이 파괴되어 풀로 돌아갈 때 코루틴 확실히 끄기!
        if (_magmaCoroutine != null)
        {
            StopCoroutine(_magmaCoroutine);
            _magmaCoroutine = null;
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

        CheckBoundaries();
    }
    // 물리 엔진 일시정지 로직
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
        switch(Stat.Type)
        {
            case MeteorType.FractureMeteor:
                SpawnFragment();
                break;
            case MeteorType.SludgeMeteor:
                SpawnSludgePuddle();
                break;
        }
        Managers.Level.AddExp(Stat.Exp.TotalValue);
        Managers.Pool.Release(gameObject);
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

    private void SpawnFragment()
    {
        // 2개 ~ 4개의 파편을 흩뿌림
        int fragmentCount = Random.Range(2, 5);

        for (int i = 0; i < fragmentCount; i++)
        {
            MeteorController fragment = Managers.Pool.Get<MeteorController>(Define.Pool.Meteor);
            if (fragment != null)
            {
                // 현재 죽은 위치에서, Fragment 타입으로, 사방으로 튀게 Init!
                fragment.Init(transform.position,Managers.Stat.GetMeteorStat(MeteorType.Fragment));
            }
        }
    }
    // 추가: 0.5초마다 현재 위치에 마그마 장판을 생성하는 코루틴
    private IEnumerator CoDropMagma()
    {
        while (true)
        {
            // 0.5초 대기 (이 간격을 조절하면 장판이 더 촘촘하거나 듬성듬성 깔립니다)
            yield return new WaitForSeconds(0.5f);

            // 게임 플레이 중일 때만 장판 생성
            if (Managers.Game.currentGameState == GameState.Playing)
            {
                // 풀링 매니저에서 마그마 장판 꺼내기 (Pool enum에 MagmaPuddle 추가 필요)
                MagmaPuddle puddle = Managers.Pool.Get<MagmaPuddle>(Define.Pool.MagmaPuddle);
                if (puddle != null)
                {
                    // 장판의 데미지는 운석 데미지의 절반(예시)으로 세팅
                    float puddleDamage = Stat.Damage.TotalValue * 0.5f;
                    puddle.Init(transform.position, puddleDamage);
                }
            }
        }
    }

    private void SpawnSludgePuddle()
    {
        SludgePuddle puddle = Managers.Pool.Get<SludgePuddle>(Define.Pool.SludgePuddle);
        if (puddle != null)
        {
            puddle.Init(transform.position);
        }
    }
}
