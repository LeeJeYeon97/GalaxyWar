using UnityEngine;

public class PlayerScreenWrap : MonoBehaviour
{
    private Camera _mainCamera;
    private Vector2 _screenBounds;
    private float _objectWidth;
    private float _objectHeight;

    void Start()
    {
        _mainCamera = Camera.main;

        // 1. 화면의 오른쪽 위 모서리 좌표(1, 1)를 월드 좌표로 변환하여 맵의 크기(반지름)를 구함
        // (0,0)은 화면 중앙이라고 가정 (Orthographic Camera)
        _screenBounds = _mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, _mainCamera.transform.position.z));

        // 2. 플레이어의 스프라이트 크기만큼 여유를 둠 (몸이 완전히 나간 뒤에 이동하게)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            _objectWidth = sr.bounds.extents.x; // 너비의 절반
            _objectHeight = sr.bounds.extents.y; // 높이의 절반
        }
    }

    // 물리 이동을 쓰는 플레이어라면 FixedUpdate가 더 부드러울 수 있음
    void FixedUpdate()
    {
        Vector3 viewPos = transform.position;
        bool isWrapped = false;

        // --- 가로(X축) 체크 ---
        // 오른쪽 끝을 벗어났다면 -> 왼쪽 끝으로
        if (viewPos.x > _screenBounds.x + _objectWidth)
        {
            viewPos.x = -_screenBounds.x - _objectWidth;
            isWrapped = true;
        }
        // 왼쪽 끝을 벗어났다면 -> 오른쪽 끝으로
        else if (viewPos.x < -_screenBounds.x - _objectWidth)
        {
            viewPos.x = _screenBounds.x + _objectWidth;
            isWrapped = true;
        }

        // --- 세로(Y축) 체크 ---
        // 위쪽 끝을 벗어났다면 -> 아래쪽 끝으로
        if (viewPos.y > _screenBounds.y + _objectHeight)
        {
            viewPos.y = -_screenBounds.y - _objectHeight;
            isWrapped = true;
        }
        // 아래쪽 끝을 벗어났다면 -> 위쪽 끝으로
        else if (viewPos.y < -_screenBounds.y - _objectHeight)
        {
            viewPos.y = _screenBounds.y + _objectHeight;
            isWrapped = true;
        }

        // 위치 적용
        if (isWrapped)
        {
            transform.position = viewPos;
        }
    }
}
