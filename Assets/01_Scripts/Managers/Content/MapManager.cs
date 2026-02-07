using UnityEngine;

public class MapManager
{
    [Header("Play Zone Settings (%)")]
    [Range(0, 0.5f)] public float topMargin = 0f;    // 상단 10% 비움
    [Range(0, 0.5f)] public float bottomMargin = 0f; // 하단 15% 비움

    public float wallThickness = 0.03f; // 벽의 두께
    public Color wallColor = new Color32(40, 123, 255, 255); // 벽의 색상 설정
    public Sprite wallSprite;
    private Camera mainCam;

    public Vector2 PlayZoneMin { get; private set; }
    public Vector2 PlayZoneMax { get; private set; }

    Vector3 fullzoneMin;
    Vector3 fullzoneMax;

    public Transform root;
    public void Init()
    {
        mainCam = Camera.main;
        // 벽 스프라이트
        wallSprite = Managers.Resource.Load<Sprite>("Sprites/Wall");

        root = new GameObject("@Map").transform;
        CalculatePlayZone();
        GenerateWalls();
    }

    void CalculatePlayZone()
    {
        // Viewport 좌표를 월드로 변환하여 실제 플레이 영역의 경계를 구함
        fullzoneMin = mainCam.ViewportToWorldPoint(new Vector3(0, bottomMargin, mainCam.nearClipPlane));
        fullzoneMax = mainCam.ViewportToWorldPoint(new Vector3(1, 1 - topMargin, mainCam.nearClipPlane));

        PlayZoneMin = new Vector2(fullzoneMin.x + wallThickness, fullzoneMin.y + wallThickness);
        PlayZoneMax = new Vector2(fullzoneMax.x - wallThickness, fullzoneMax.y - wallThickness);
    }

    void GenerateWalls()
    {

        float screenWidth = fullzoneMax.x - fullzoneMin.x;
        float screenHeight = fullzoneMax.y - fullzoneMin.y;
        Vector2 center = (PlayZoneMin + PlayZoneMax) / 2f;
        float half = wallThickness / 2f;
        // TopWall: PlayZone 상단 끝에서 두께 절반만큼 위에 배치
        CreateWall("TopWall", new Vector2(center.x, PlayZoneMax.y + half), new Vector2(screenWidth, wallThickness));

        // BottomWall: PlayZone 하단 끝에서 두께 절반만큼 아래에 배치
        CreateWall("BottomWall", new Vector2(center.x, PlayZoneMin.y - half), new Vector2(screenWidth, wallThickness));

        // LeftWall: PlayZone 왼쪽 끝에서 두께 절반만큼 왼쪽에 배치
        CreateWall("LeftWall", new Vector2(PlayZoneMin.x - wallThickness, center.y), new Vector2(wallThickness, screenHeight));

        // RightWall: PlayZone 오른쪽 끝에서 두께 절반만큼 오른쪽에 배치
        CreateWall("RightWall", new Vector2(PlayZoneMax.x + wallThickness, center.y), new Vector2(wallThickness, screenHeight));

    }

    void CreateWall(string name, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = position;
        wall.transform.parent = root; // 정리를 위해 현재 오브젝트의 자식으로 등록

        // 1. 스프라이트 렌더러 추가 및 설정
        SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
        sr.sprite = wallSprite; // 인스펙터에서 넣은 이미지 적용
        sr.color = wallColor;   // 색상 적용

        // 2. 이미지를 벽 크기에 맞게 늘리기 (중요!)
        // 기본 1x1 크기의 스프라이트를 전달받은 size만큼 스케일 조정
        wall.transform.localScale = new Vector3(size.x, size.y, 1f);
        wall.tag = "Wall";
        // 3. 콜라이더 추가 (스케일이 반영되므로 size를 (1,1)로 두면 오브젝트 크기에 맞게 생성됨)
        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();

        // 4. 라인 렌더러를 위한 Layer설정
        wall.layer = LayerMask.NameToLayer("Wall");

    }

    void OnDrawGizmos()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // 1. 계산용 기본 좌표 구하기 (에디터 실시간 반영용)
        Vector3 fMin = mainCam.ViewportToWorldPoint(new Vector3(0, bottomMargin, 10));
        Vector3 fMax = mainCam.ViewportToWorldPoint(new Vector3(1, 1 - topMargin, 10));

        // 2. FullZone 그리기 (화면 마진만 적용된 전체 틀)
        // 빨간색 점선 스타일로 그려서 PlayZone과 구분합니다.
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube((fMin + fMax) / 2f, fMax - fMin);

        // 3. PlayZone 좌표 계산 (벽 두께 반영)
        // 현재 코드 로직상 벽 두께만큼 안으로 들어온 위치
        Vector3 pMin = new Vector3(fMin.x + wallThickness, fMin.y + wallThickness, 0);
        Vector3 pMax = new Vector3(fMax.x - wallThickness, fMax.y - wallThickness, 0);

        // 4. PlayZone 그리기 (실제 공과 벽돌이 노는 영역)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((pMin + pMax) / 2f, pMax - pMin);

        //// 5. 파란색 격자 (PlayZone 내부에만 그리기)
        //Gizmos.color = new Color(0, 1, 1, 0.5f); // 약간 투명한 하늘색
        //for (int i = 0; i <= 10; i++)
        //{
        //    float t = i / 10f;
        //    // 가로선
        //    float y = Mathf.Lerp(pMin.y, pMax.y, t);
        //    Gizmos.DrawLine(new Vector3(pMin.x, y, 0), new Vector3(pMax.x, y, 0));
        //    // 세로선
        //    float x = Mathf.Lerp(pMin.x, pMax.x, t);
        //    Gizmos.DrawLine(new Vector3(x, pMin.y, 0), new Vector3(x, pMax.y, 0));
        //}
    }
}
