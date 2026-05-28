using UnityEngine;
using static Define;

public class MeteorMovement : MonoBehaviour
{
    private MeteorController _controller;
    public Rigidbody2D _rb { get; private set; }

    public Stat currentSpeed = new Stat();
    private Vector2 _moveDir;
    private float _baseAngularVelocity;

    public bool _hasEnteredView = false;
    private float _checkOffset = 2.0f;

    private Vector2 _savedVelocity;
    private float _savedAngularVelocity;
    private bool _physicsFrozenByPause = false;

    //  1. 유도탄 여부를 저장할 변수 추가
    private bool _isChasing = false;

    private void Awake()
    {
        _controller = GetComponent<MeteorController>();
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 pos, MeteorStat stat)
    {
        _hasEnteredView = false;
        transform.position = pos;

        //  2. 스탯에서 추적 여부를 받아와 저장합니다.
        // (만약 targetChase가 bool 타입이 아니라 수치형 Stat이라면 stat.targetChase.TotalValue > 0f 등으로 조건부여를 해주세요)
        _isChasing = stat.targetChase;

        _moveDir = ((Vector2)Managers.Game._player.transform.position - pos).normalized;
        float speed = UnityEngine.Random.Range(stat.MinSpeed.TotalValue, stat.MaxSpeed.TotalValue);
        float finalSpeed = Managers.Stage.GetCalculatedMeteorSpeed(speed);
        currentSpeed.Init(finalSpeed);
        _baseAngularVelocity = UnityEngine.Random.Range(-100f, 100f);

        _rb.simulated = true;
        UpdateVelocity();
    }

    public void UpdateVelocity()
    {
        _rb.linearVelocity = _moveDir * currentSpeed.TotalValue;
        _rb.angularVelocity = (currentSpeed.TotalValue == 0) ? 0f : _baseAngularVelocity;
    }

    private void FixedUpdate()
    {
        bool isPaused = (Managers.Game.currentGameState == GameState.Pause);
        bool isGameOver = (Managers.Game.currentGameState == GameState.GameOver);
        bool isGameClear = (Managers.Game.currentGameState == GameState.GameClear);

        if ((isPaused || isGameOver || isGameClear) && !_physicsFrozenByPause)
        {
            _savedVelocity = _rb.linearVelocity;
            _savedAngularVelocity = _rb.angularVelocity;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.simulated = false;
            _physicsFrozenByPause = true;
        }
        else if (!isPaused && !isGameOver && !isGameClear && _physicsFrozenByPause)
        {
            _rb.simulated = true;
            _rb.linearVelocity = _savedVelocity;
            _rb.angularVelocity = _savedAngularVelocity;
            _physicsFrozenByPause = false;
        }

        if (!isPaused && !isGameOver && !isGameClear)
        {
            //  3. 유도탄일 경우 방향을 계속 갱신합니다.
            if (_isChasing)
            {
                UpdateChaseDirection();
            }
            CheckBoundaries();
        }
    }
    // 4. 플레이어 방향으로 꺾어주는 함수 추가
    private void UpdateChaseDirection()
    {
        if (Managers.Game._player == null) return;

        // 플레이어의 현재 위치를 향해 방향 재계산
        Vector2 targetPos = Managers.Game._player.transform.position;
        Vector2 targetDir = (targetPos - (Vector2)transform.position).normalized;

        // 바로 꽂히는 대신, 0.05f 등의 수치로 '서서히' 꺾이게 만듭니다. (수치가 낮을수록 둔해집니다)
        float turnSpeed = 0.05f;
        _moveDir = Vector2.Lerp(_moveDir, targetDir, turnSpeed).normalized;

        UpdateVelocity();
    }
    private void CheckBoundaries()
    {
        Vector3 pos = transform.position;
        var min = Managers.Map.PlayZoneMin;
        var max = Managers.Map.PlayZoneMax;

        bool isInView = pos.x > min.x && pos.x < max.x && pos.y > min.y && pos.y < max.y;
        if (isInView) _hasEnteredView = true;

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