using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JustRotate : MonoBehaviour {

    // 🌟 1. 선택할 수 있는 축의 종류를 정의합니다 (열거형)
    public enum RotationAxis { X, Y, Z }

    [Header("설정")]
    public bool canRotate = true;
    public float speed = 10f;

    //  2. 인스펙터에서 선택할 수 있는 드롭다운 변수
    public RotationAxis selectedAxis = RotationAxis.Z;

    void Update()
    {
        if (!canRotate) return;

        //  3. 선택된 축에 따라 회전 방향(Vector3)을 결정합니다.
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

        //  4. 결정된 축 방향으로 회전!
        transform.Rotate(axisVector * speed * Time.deltaTime);
    }
}
