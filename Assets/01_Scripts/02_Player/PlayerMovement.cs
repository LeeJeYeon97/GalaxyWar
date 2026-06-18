using UnityEngine;
using static Define;

public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    private PlayerController _player;
    private Rigidbody2D _rb;

    // 드래그 관련 변수
    private Vector2 dragStartPos;
    private Vector2 dragPos;
    private Vector2 dragDir;

    // 일시정지 대응 변수
    private Vector2 _savedVelocity;
    private bool _physicsFrozenByPause = false;

    [Header("우주선 이동 세팅")]
    public float movePower = 1f;
    public float rotatePower = 15f;
    public float acceleration = 3f; //  값이 작을수록 더 늦게 최고속도에 도달 (무거운 우주선)
    public float deceleration = 2f; //값이 작을수록 손을 떼도 얼음판처럼 멀리 미끄러짐

    [Header("피격(넉백) 세팅")]
    private Vector2 _externalForce = Vector2.zero;
    public float knockbackDecay = 5f;

    public void Init(PlayerController player)
    {
        _player = player;
        _rb = GetComponent<Rigidbody2D>();

        // 중력 0, 물리 엔진에 의한 멋대로 회전 금지 (관성은 코드로 직접 제어)
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

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

        // 1. 일시정지 처리
        if (Managers.Game.currentGameState != GameState.Playing && !_physicsFrozenByPause)
        {
            _savedVelocity = _rb.linearVelocity;
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
            _physicsFrozenByPause = true;
            return;
        }
        else if (Managers.Game.currentGameState == GameState.Playing && _physicsFrozenByPause)
        {
            _rb.simulated = true;
            _rb.linearVelocity = _savedVelocity;
            _physicsFrozenByPause = false;
        }

        if (_player.currentState != PlayerState.Playing)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // 2. 이동 및 회전
        Move();
        Rotate();
    }

    #region Input Event
    void OnDragStart(Vector2 pos) { if (Managers.Game.currentGameState == GameState.Playing) dragStartPos = pos; }
    void OnDragUpdate(Vector2 pos)
    {
        if (Managers.Game.currentGameState == GameState.Playing)
        {
            dragPos = pos;
            dragDir = (dragPos - dragStartPos).normalized;
        }
    }
    void OnDragRelease() { dragDir = Vector2.zero; }
    #endregion

    #region Movement Logic
    private void Move()
    {
        // 1. 패드 입력에 따른 최종 목표 속도
        Vector2 targetVelocity = dragDir * (_player.Stat.speed.TotalValue * movePower);

        // 2. 가속할지 감속할지 결정
        float currentAccelRate = (dragDir == Vector2.zero) ? deceleration : acceleration;

        //  [핵심 추가] 고속 주행 중 반대 방향이나 측면으로 확 틀었을 때(급회전)
        // 기존 관성 때문에 붕 뜨는 현상을 막기 위해 가속도를 순간적으로 펌핑합니다.
        if (dragDir != Vector2.zero && _rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            float dot = Vector2.Dot(_rb.linearVelocity.normalized, dragDir);
            if (dot < 0.3f) // 현재 이동 방향과 입력 방향의 각도가 크게 틀어졌을 때 (약 70도 이상)
            {
                currentAccelRate *= 2.5f; // 가속 레이트를 올려서 기존 관성을 빠르게 잡아먹고 새 방향으로 전환
            }
        }

        // 3. 우주선 관성 계산
        Vector2 currentMoveVelocity = _rb.linearVelocity - _externalForce;
        Vector2 nextVelocity = Vector2.Lerp(currentMoveVelocity, targetVelocity, currentAccelRate * Time.fixedDeltaTime);

        // 4. 외부 충격(넉백) 서서히 감소
        _externalForce = Vector2.Lerp(_externalForce, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);

        // 5. 최종 속도 주입
        _rb.linearVelocity = nextVelocity + _externalForce;
    }

    private void Rotate()
    {
        Vector2 targetLookDir = dragDir != Vector2.zero ? dragDir : _rb.linearVelocity.normalized;
        if (targetLookDir.sqrMagnitude < 0.01f) return;

        float targetAngle = Mathf.Atan2(targetLookDir.y, targetLookDir.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle - 90f);

        //  [핵심 변경] Lerp를 버리고 RotateTowards를 사용합니다!
        // 목표 각도 근처에 도달해도 속도가 줄어들지 않고 정속으로 딱딱하게 회전력을 끝까지 밀어붙입니다.
        //  주의: RotateTowards의 세 번째 인자는 '초당 회전 각도(Degrees)'입니다.
        // 따라서 유니티 인스펙터 창에서 rotatePower 값을 최소 [ 720 ~ 1080 ] 정도로 대폭 올려주셔야 합니다!
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotatePower * Time.fixedDeltaTime);
    }
    #endregion

    public void AddKnockback(Vector2 force)
    {
        _externalForce += force;
    }
}