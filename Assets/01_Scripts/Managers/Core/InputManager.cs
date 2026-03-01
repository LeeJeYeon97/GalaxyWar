using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{

    // 외부에서 구독할 이벤트들
    public Action<Vector2> OnDragStarted;   // 드래그 시작 (좌표)
    public Action<Vector2> OnDragging;      // 드래그 중 (좌표)
    public Action OnDragEnded;  // 드래그 끝 (좌표)

    private Camera mainCam;

    private InputSystem_Actions control;
    private bool _isPressed = false; // 드래그 중인지 판별용
    public void Init()
    {
        if (control != null) return;

        control = new InputSystem_Actions();

        // 람다 대신 직접 메서드 연결 (메모리 누수 방지)
        control.Player.Attack.started += OnTouchStartedInternal;
        control.Player.Attack.canceled += OnTouchCanceledInternal;

        control.Enable();
    }

    private void OnDisable()
    {
        if (control == null) return;

        control.Player.Attack.started -= OnTouchStartedInternal;
        control.Player.Attack.canceled -= OnTouchCanceledInternal;
        control.Disable();
    }

    private void Update()
    {
        if (control == null || !_isPressed) return;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // Point는 이제 마우스와 터치 위치를 모두 포함하는 'Pointer' 값을 읽어옵니다.
        Vector2 screenPos = control.Player.Point.ReadValue<Vector2>();
        Vector2 worldPos = mainCam.ScreenToWorldPoint(screenPos);
        OnDragging?.Invoke(worldPos);
    }

    private void OnTouchStartedInternal(InputAction.CallbackContext ctx)
    {
        _isPressed = true;
        Debug.Log("터치 들어옴");
        if (mainCam == null) mainCam = Camera.main;

        Vector2 screenPos = control.Player.Point.ReadValue<Vector2>();
        Vector2 worldPos = mainCam.ScreenToWorldPoint(screenPos);
        OnDragStarted?.Invoke(worldPos);
    }

    private void OnTouchCanceledInternal(InputAction.CallbackContext ctx)
    {
        Debug.Log("터치 나감");
        _isPressed = false;
        OnDragEnded?.Invoke();
    }

}
