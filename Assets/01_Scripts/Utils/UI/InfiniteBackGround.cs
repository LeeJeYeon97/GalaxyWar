using UnityEngine;
using UnityEngine.UI;

public class InfiniteBackGround : MonoBehaviour
{
    [Header("연결할 컴포넌트")]
    public RawImage backgroundImage;
    public Transform playerTransform; // 플레이어 위치 (따라갈 대상)

    [Header("스크롤 설정")]
    public float parallaxSpeed = 0.05f;  // 플레이어 이동 시 배경이 반응하는 속도 (수치가 작을수록 배경이 멀리 있는 느낌)

    [Header("회전 감도 설정")]
    public bool useRotation = true;
    [Range(0f, 1f)]
    public float rotationMultiplier = 0.3f;
    public float rotationSmoothSpeed = 5f;

    // ★ 튀김 현상 방지를 위한 내부 변수 추가
    private float _prevPlayerRotation;  // 이전 프레임의 플레이어 각도
    private float _currentBgRotation;   // 배경의 진짜 누적 각도
    private bool _isInitialized = false;

    private void Start()
    {
        playerTransform = Managers.Game._player.transform;
    }
    void Update()
    {
        if (backgroundImage == null || playerTransform == null) return;

        // 1. 이동 스크롤
        float finalOffsetX = playerTransform.position.x * parallaxSpeed;
        float finalOffsetY = playerTransform.position.y * parallaxSpeed;
        backgroundImage.uvRect = new Rect(finalOffsetX, finalOffsetY, 1f, 1f);

        // 2. 부드러운 회전 적용 (튀김 버그 완벽 해결)
        if (useRotation)
        {
            float currentPlayerRotation = playerTransform.eulerAngles.z;

            // 게임 시작 직후 딱 한 번, 이전 각도를 현재 각도로 초기화해줍니다.
            if (!_isInitialized)
            {
                _prevPlayerRotation = currentPlayerRotation;
                _isInitialized = true;
            }

            // ★ 핵심: 플레이어가 '이번 프레임에 실제로 움직인 순수 각도'를 구합니다.
            // Mathf.DeltaAngle은 359 -> 0이 될 때 -359가 아니라 +1이라고 똑똑하게 계산해 줍니다!
            float deltaRotation = Mathf.DeltaAngle(_prevPlayerRotation, currentPlayerRotation);

            // 플레이어가 움직인 만큼 배율을 곱해서 배경 각도에 '누적'시킵니다. (반대로 돌아야 하니 빼줍니다)
            _currentBgRotation -= deltaRotation * rotationMultiplier;

            // 목표 회전값 세팅
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, _currentBgRotation);

            // 부드럽게 회전 적용
            backgroundImage.rectTransform.localRotation = Quaternion.Lerp(
                backgroundImage.rectTransform.localRotation,
                targetRotation,
                Time.deltaTime * rotationSmoothSpeed
            );

            // 다음 프레임 계산을 위해 현재 각도를 저장해 둡니다.
            _prevPlayerRotation = currentPlayerRotation;
        }
    }
}
