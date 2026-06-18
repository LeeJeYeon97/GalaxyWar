using UnityEngine;

namespace Hovl
{
    [ExecuteAlways]
    public class HS_ScreenEffect : MonoBehaviour
    {
        public ParticleSystem screenEffect;
        public Camera sourceCamera;

        // 카메라 앞으로 얼마나 띄울지 결정하는 거리
        public float fallbackDistance = 10f; // 2D 환경이면 카메라가 -10에 있으니 10정도가 적당할 수 있습니다.

        //  [수정됨] 이제 부모 자식 관계를 사용하지 않으므로 불필요한 변수들은 제거 또는 주석 처리해도 됩니다.
        // public bool snapOnStart = true;
        // public bool parentToCameraOnStart = true;

        void Reset()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;
        }

        void OnEnable()
        {
            if (sourceCamera == null)
                sourceCamera = Camera.main;

            // OnEnable에 있던 복잡한 SetParent 로직을 과감히 삭제합니다.
            // 이제 LateUpdate에서 매 프레임 완벽하게 추적할 것입니다.
            UpdateSize();
        }

        void LateUpdate()
        {
            //  1. 매 프레임 카메라 위치를 추적 (시네머신 이동이 끝난 후인 LateUpdate가 최적)
            Camera cam = sourceCamera != null ? sourceCamera : Camera.main;
            if (cam != null)
            {
                // 카메라의 위치 + 앞으로 설정한 거리만큼 떨어져서 따라다님
                transform.position = cam.transform.position + cam.transform.forward * fallbackDistance;

                // 회전도 카메라와 완벽하게 일치시킴
                transform.rotation = cam.transform.rotation;
            }

            // 2. 화면 사이즈 갱신
            UpdateSize();
        }

        void OnValidate()
        {
            UpdateSize();
        }

        void UpdateSize()
        {
            if (screenEffect == null)
                return;

            Camera cam = sourceCamera != null ? sourceCamera : Camera.main;
            if (cam == null)
                return;

            float dist = cam.transform.InverseTransformPoint(transform.position).z;
            if (dist <= 0f)
                dist = fallbackDistance;

            float height;
            if (cam.orthographic)
            {
                height = 2f * cam.orthographicSize;
            }
            else
            {
                float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
                height = 2f * dist * Mathf.Tan(fovRad * 0.5f);
            }

            float width = height * cam.aspect;

            var main = screenEffect.main;
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(width);
            main.startSizeY = new ParticleSystem.MinMaxCurve(height);
            main.startSizeZ = new ParticleSystem.MinMaxCurve(1f);

            var shape = screenEffect.shape;
            shape.scale = new Vector3(width, height, 1f);
        }
    }
}