using UnityEngine;

public class UI_Joystick : UI_Base
{
    [Header("UI Components")]
    [SerializeField] private RectTransform container; // 조이스틱 배경
    [SerializeField] private RectTransform handle;    // 조이스틱 핸들

    [Header("Settings")]
    [SerializeField] private float radius = 100f;    // 핸들이 움직일 수 있는 최대 반경

    [Header("Indicator")]
    [SerializeField] private RectTransform indicator; // 회전시킬 하이라이트 UI
    [SerializeField] private float angleOffset = -90f; // 이미지의 기본 방향에 따른 보정값

    public override void Init()
    {
        base.Init();

        Managers.Input.OnDragStarted += ShowJoystick;
        Managers.Input.OnDragging += OnDragging;
        Managers.Input.OnDragEnded += HideJoystick;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
       
        Managers.Input.OnDragStarted -= ShowJoystick;
        Managers.Input.OnDragging -= OnDragging;
        Managers.Input.OnDragEnded -= HideJoystick;
    }
    private void ShowJoystick(Vector2 screenPos)
    {
        if (Managers.Game.currentGameState != Define.GameState.Playing) return;

        RectTransform parentRect = container.parent.GetComponent<RectTransform>();

        // [핵심 추가] Canvas의 렌더 모드가 Camera 모드일 때를 대비해 UI 카메라를 자동으로 찾아옵니다.
        Camera uiCam = null;
        Canvas canvas = container.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        //  세 번째 인자에 찾은 uiCam을 넣어 오차를 완전히 방지합니다.
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, uiCam, out Vector2 localPoint))
        {
            container.anchoredPosition = localPoint;
        }

        handle.anchoredPosition = Vector2.zero;
        gameObject.SetActive(true);
    }

    private void OnDragging(Vector2 screenPos)
    {
        if (Managers.Game.currentGameState != Define.GameState.Playing) return;

        //  여기도 마찬가지로 Canvas 모드에 따른 카메라를 매칭해줍니다.
        Camera uiCam = null;
        Canvas canvas = container.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        Vector2 localPoint;
        //  uiCam을 넣어 정확한 로컬 좌표를 뽑아냅니다.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPos, uiCam, out localPoint);

        float distance = localPoint.magnitude;
        Vector2 direction = localPoint.normalized;

        if (distance > radius)
        {
            handle.anchoredPosition = direction * radius;
        }
        else
        {
            handle.anchoredPosition = localPoint;
        }

        Vector2 _inputVector = handle.anchoredPosition / radius;
        UpdateIndicatorRotation(_inputVector);
    }

    private void HideJoystick()
    {

        handle.anchoredPosition = Vector2.zero; // 핸들 위치 초기화
        gameObject.SetActive(false);
    }


    private void UpdateIndicatorRotation(Vector2 direction)
    {
        if (Managers.Game.currentGameState != Define.GameState.Playing)
        {
            return;
        }
        if (direction == Vector2.zero) return;

        // 1. 벡터를 라디안 각도로 변환 후 도(Degree) 단위로 변경
        // Mathf.Atan2(y, x) 순서에 주의하세요.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 2. 이미지의 기본 방향 보정
        // 유니티 UI에서 0도는 오른쪽(Right)입니다. 
        // 만약 이미지가 위(Up)를 보고 있다면 -90도 정도의 오프셋이 필요할 수 있습니다.
        indicator.localEulerAngles = new Vector3(0, 0, angle + angleOffset);
    }
}
