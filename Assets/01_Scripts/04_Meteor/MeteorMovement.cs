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

    private void Awake()
    {
        _controller = GetComponent<MeteorController>();
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 pos, MeteorStat stat)
    {
        _hasEnteredView = false;
        transform.position = pos;
        _moveDir = ((Vector2)Managers.Game._player.transform.position - pos).normalized;
        currentSpeed.Init(UnityEngine.Random.Range(stat.MinSpeed.TotalValue, stat.MaxSpeed.TotalValue));
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
            CheckBoundaries();
        }
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