using UnityEngine;

public class ItemAnimation : MonoBehaviour
{
    [Header("기능 활성화 설정")]
    [SerializeField] private bool _useBobbing = true;   // 위아래로 튈 것인가?
    [SerializeField] private bool _useRotation = true;  // 빙글빙글 돌 것인가?

    [Header("통통 튀기(Bobbing) 설정")]
    [SerializeField] private float _bobbingSpeed = 3f;
    [SerializeField] private float _bobbingHeight = 0.2f;

    [Header("회전(Rotation) 설정")]
    [SerializeField] private float _rotationSpeed = 100f; // 초당 회전 각도

    private Vector3 _startPos;

    void Update()
    {
        HandleBobbing();
        HandleRotation();
    }
    public void SetStartPosition(Vector3 newPos)
    {
        _startPos = newPos;
    }
    private void HandleBobbing()
    {
        if (!_useBobbing) return;

        // 위아래 부드러운 이동 계산
        float newY = _startPos.y + (Mathf.Sin(Time.time * _bobbingSpeed) * _bobbingHeight);
        transform.localPosition = new Vector3(_startPos.x, newY, _startPos.z);
    }

    private void HandleRotation()
    {
        if (!_useRotation) return;

        // Z축(2D 게임의 앞방향)을 기준으로 회전
        // 3D 느낌을 내고 싶다면 Vector3.up 등을 사용해도 됩니다.
        transform.Rotate(Vector3.forward * _rotationSpeed * Time.deltaTime);
    }
}