using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class AttackRangeIndicator : MonoBehaviour
{
    
    private LineRenderer _lineRenderer;

    [Header("원형 라인 설정")]
    [Tooltip("원을 이루는 점의 개수. 높을수록 원이 부드럽습니다.")]
    public int segments = 100;
    public float radius = 3f; // 공격 범위 (반지름)
    public float lineWidth = 0.05f; // 선의 두께 (아주 얇게 설정 가능)
    public float rotationSpeed = 1.0f;

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    public void SetupLineRenderer()
    {
        if (_lineRenderer == null) return;

        //  [수정] 마지막 점을 시작점 위에 덮어쓰기 위해 +1을 해줍니다.
        _lineRenderer.positionCount = segments + 1;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;


        // (주의) 씬 창에서 미리보기 할 때는 플레이어 스탯을 가져올 수 없으므로(에러 발생), 
        // 게임 실행 중(Application.isPlaying)일 때만 스탯에서 반지름을 가져오게 방어 코드를 칩니다.
        if (Application.isPlaying && Managers.Game != null && Managers.Game._player != null)
        {
            radius = Managers.Game._player.Stat.shotRange.TotalValue;
        }

        _lineRenderer.useWorldSpace = false;
        //  [수정] loop를 true로 하면 텍스처(점선)가 끝부분에서 기괴하게 늘어날 수 있으므로 false로 끕니다.
        _lineRenderer.loop = false;

        DrawCircle();
    }

    public void DrawCircle()
    {
        float angle = 0f;
        float angleStep = 360f / segments;

        //  [수정] i < segments 가 아니라 i <= segments 로 바꿔서 마지막 점까지 확실하게 찍어줍니다.
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            float y = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;

            _lineRenderer.SetPosition(i, new Vector3(x, y, 0f));

            angle += angleStep;
        }
    }

    //  레벨업해서 공격 범위가 넓어졌을 때 이 함수를 호출해주세요!
    public void UpdateRange(float newRadius)
    {
        radius = newRadius;
        DrawCircle();
    }
    //  라인을 숨기는 함수 완성!
    public void HideCircle()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }
    }

    //  보스가 죽거나 다시 보여야 할 때 호출할 함수 추가!
    public void ShowCircle()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = true;
            // 혹시 그 사이에 공격 범위가 바뀌었을 수도 있으니 다시 그려줍니다.
            SetupLineRenderer();
        }
    }
}