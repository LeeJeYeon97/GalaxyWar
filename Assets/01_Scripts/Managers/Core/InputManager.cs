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
    public void Init()
    {
        // 1. 이미 존재한다면 중복 생성 방지
        if (control != null) return;

        control = new InputSystem_Actions();

        control.Player.Attack.started += ctx => OnAttackStarted();
        control.Player.Attack.canceled += ctx => OnAttackCanceled();

        // 3. 여기서 직접 활성화 (OnEnable 대신)
        control.Enable();
    }
    private void OnDisable()
    {
        control.Player.Attack.started -= ctx => OnAttackStarted();
        control.Player.Attack.canceled -= ctx => OnAttackCanceled();
        control.Disable();
    }
    public void Clear()
    {
        mainCam = null;
    }
    private void Update()
    {
        if (control == null) return;
        // 카메라가 null이라면 현재 씬의 메인 카메라를 새로 찾습니다.
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // 마우스/터치 좌표는 매 프레임 읽어와서 이벤트를 쏴줍니다.
        Vector2 screenPos = control.Player.Point.ReadValue<Vector2>();
        Vector2 worldPos = mainCam.ScreenToWorldPoint(screenPos);
        OnDragging?.Invoke(worldPos);
    }

    private void OnAttackStarted()
    {
        if (mainCam == null) mainCam = Camera.main;
        // 좌표를 읽어와서 반드시 월드 좌표로 변환해서 쏴줍니다.
        Vector2 screenPos = control.Player.Point.ReadValue<Vector2>();
        Vector2 worldPos = mainCam.ScreenToWorldPoint(screenPos);
        OnDragStarted?.Invoke(worldPos);
    }

    private void OnAttackCanceled()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;
        Vector2 screenPos = control.Player.Point.ReadValue<Vector2>();
        OnDragEnded?.Invoke();
    }
}
