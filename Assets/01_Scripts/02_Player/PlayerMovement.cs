using UnityEngine;
using static Define;

public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    private PlayerController _player;   // 본체 참조
    private Rigidbody2D _rb;

    // 드래그 관련 변수
    private Vector2 dragStartPos;
    private Vector2 dragPos;
    private Vector2 dragDir;

    // 일시정지 대응 변수
    private Vector2 _savedVelocity;
    private float _savedAngularVelocity;
    private bool _physicsFrozenByPause = false;

    public float movePower;
    public float rotatePower;

    // 본체(PlayerController)에서 초기화할 때 호출해 줍니다.
    public void Init(PlayerController player)
    {
        _player = player;
        _rb = GetComponent<Rigidbody2D>();

        _rb.gravityScale = 0f;
        _rb.linearDamping = 0.5f;

        // 입력 이벤트 구독
        Managers.Input.OnDragStarted -= OnDragStart;
        Managers.Input.OnDragStarted += OnDragStart;
        Managers.Input.OnDragging -= OnDragUpdate;
        Managers.Input.OnDragging += OnDragUpdate;
        Managers.Input.OnDragEnded -= OnDragRelease;
        Managers.Input.OnDragEnded += OnDragRelease;
    }

    private void OnDestroy()
    {
        // 모듈이 파괴될 때 이벤트 구독 해제
        if (Managers.Input != null)
        {
            Managers.Input.OnDragStarted -= OnDragStart;
            Managers.Input.OnDragging -= OnDragUpdate;
            Managers.Input.OnDragEnded -= OnDragRelease;
        }
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        // 1. 일시정지 및 게임오버 물리 멈춤 로직
        if (Managers.Game.currentGameState != GameState.Playing
            && !_physicsFrozenByPause)
        {
            _savedVelocity = _rb.linearVelocity;
            _savedAngularVelocity = _rb.angularVelocity;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.simulated = false;
            _physicsFrozenByPause = true;
            return;
        }
        else if (Managers.Game.currentGameState == GameState.Playing
            && _physicsFrozenByPause)
        {
            _rb.simulated = true;
            _rb.linearVelocity = _savedVelocity;
            _rb.angularVelocity = _savedAngularVelocity;
            _physicsFrozenByPause = false;
        }

        // 2. 플레이 중이 아니면 멈춤
        if (_player.currentState != PlayerState.Playing)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // 3. 실제 이동 및 회전 처리
        Move();
        Rotate();
    }

    #region Input Event
    void OnDragStart(Vector2 pos)
    {
        if (Managers.Game.currentGameState != GameState.Playing) return;
        dragStartPos = pos;
    }

    void OnDragUpdate(Vector2 pos)
    {
        if (Managers.Game.currentGameState != GameState.Playing) return;
        dragPos = pos;
        dragDir = (dragPos - dragStartPos).normalized;
    }

    void OnDragRelease()
    {
        dragDir = Vector2.zero;
    }
    #endregion

    #region Movement Logic
    private void Move()
    {
        if (dragDir == Vector2.zero) return;

        // 본체의 Stat을 가져와서 계산
        float finalForce = _player.Stat.speed.TotalValue * movePower;
        _rb.AddForce(dragDir * finalForce, ForceMode2D.Force);

        if (_rb.linearVelocity.magnitude > _player.Stat.speed.TotalValue)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * _player.Stat.speed.TotalValue;
        }
    }

    private void Rotate()
    {
        Vector2 targetLookDir = Vector2.zero;

        if (dragDir != Vector2.zero)
        {
            targetLookDir = dragDir;
        }
        else if (_rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            targetLookDir = _rb.linearVelocity.normalized;
        }

        if (targetLookDir == Vector2.zero) return;

        float targetAngle = Mathf.Atan2(targetLookDir.y, targetLookDir.x) * Mathf.Rad2Deg;
        float finalAngle = targetAngle - 90f;

        _rb.MoveRotation(Mathf.LerpAngle(_rb.rotation, finalAngle, Time.fixedDeltaTime * rotatePower));
    }
    #endregion
}
