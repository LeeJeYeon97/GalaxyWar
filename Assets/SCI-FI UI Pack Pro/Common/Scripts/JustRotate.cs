using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JustRotate : MonoBehaviour {

    //  1. 선택할 수 있는 축의 종류를 정의합니다 (열거형)
    public enum RotationAxis { X, Y, Z }

    [Header("설정")]
    public bool canRotate = true;
    public float speed = 100f;

    //  2. 인스펙터에서 선택할 수 있는 드롭다운 변수
    public RotationAxis selectedAxis = RotationAxis.Z;

    // 독립적인 회전값을 저장할 변수
    private float _currentAngle = 0f;
    //  처음에 설정해둔 삐딱한 각도(초기 회전값)를 기억할 변수
    private Quaternion _initialRotation;

    void Start()
    {
        // 게임 시작 시점의 절대 회전값을 기억해둡니다.
        _initialRotation = transform.rotation;
    }

    // Update 대신 LateUpdate를 사용해야 부모가 회전한 이후에 내 회전을 덮어씌울 수 있습니다!
    void LateUpdate()
    {
        if (!canRotate) return;

        // 1. 회전할 각도를 누적합니다.
        _currentAngle += speed * Time.deltaTime;

        Vector3 axisVector = Vector3.zero;

        switch (selectedAxis)
        {
            case RotationAxis.X:
                axisVector = Vector3.right;   // (1, 0, 0)
                break;
            case RotationAxis.Y:
                axisVector = Vector3.up;      // (0, 1, 0)
                break;
            case RotationAxis.Z:
                axisVector = Vector3.forward; // (0, 0, 1)
                break;
        }

        // 2. 초기 회전값에 현재 회전할 각도를 더해줍니다.
        Quaternion independentRotation = _initialRotation * Quaternion.AngleAxis(_currentAngle, axisVector);

        // 3. transform.localRotation이 아닌 transform.rotation(월드 회전)을 강제로 덮어씌움!
        transform.rotation = independentRotation;
    }
}
