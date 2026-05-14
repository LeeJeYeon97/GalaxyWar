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

    private Transform topWall, bottomWall, leftWall, rightWall;

    public Transform root;
    public void Init()
    {
        mainCam = Camera.main;
        wallSprite = Managers.Resource.Load<Sprite>("Sprites/Wall");
        root = new GameObject("@Map").transform;

        // 1. 처음 한 번만 벽을 생성하여 변수에 할당
        GenerateInitialWalls();

        // 2. 현재 카메라 사이즈에 맞춰 초기 배치
        UpdateMap();
    }

    // 벽을 처음에 생성하는 로직
    void GenerateInitialWalls()
    {
        topWall = CreateWall("TopWall");
        bottomWall = CreateWall("BottomWall");
        leftWall = CreateWall("LeftWall");
        rightWall = CreateWall("RightWall");
    }

    //  핵심: 카메라 사이즈가 변할 때마다 호출될 함수
    public void UpdateMap()
    {
        CalculatePlayZone(); // 1. 좌표 재계산 (mainCam 사이즈 반영)
        RepositionWalls();   // 2. 벽 위치 및 크기 재설정
    }

    void CalculatePlayZone()
    {
        // 1. 카메라의 세로 크기 계산 (Orthographic Size의 2배)
        float worldHeight = mainCam.orthographicSize * 2f;

        // 2. [핵심] 가로 크기는 유니티 카메라에 묻지 않고 9:16 비율로 직접 계산!
        float worldWidth = worldHeight * (9f / 19.5f);

        // 3. 카메라의 현재 중심 위치 가져오기
        Vector3 camPos = mainCam.transform.position;

        // 4. 마진(여백)을 적용하여 완벽한 외곽선 좌표 구하기
        float leftX = camPos.x - (worldWidth / 2f);
        float rightX = camPos.x + (worldWidth / 2f);

        float bottomY = camPos.y - mainCam.orthographicSize + (worldHeight * bottomMargin);
        float topY = camPos.y + mainCam.orthographicSize - (worldHeight * topMargin);

        fullzoneMin = new Vector3(leftX, bottomY, 0);
        fullzoneMax = new Vector3(rightX, topY, 0);

        // 5. 벽 두께만큼 안쪽으로 밀어넣어 실제 플레이 존(PlayZone) 설정
        PlayZoneMin = new Vector2(fullzoneMin.x + wallThickness, fullzoneMin.y + wallThickness);
        PlayZoneMax = new Vector2(fullzoneMax.x - wallThickness, fullzoneMax.y - wallThickness);
    }

    void RepositionWalls()
    {
        float screenWidth = fullzoneMax.x - fullzoneMin.x;
        float screenHeight = fullzoneMax.y - fullzoneMin.y;
        Vector2 center = (PlayZoneMin + PlayZoneMax) / 2f;
        float half = wallThickness / 2f;

        // 추가된 핵심: 어떤 스프라이트를 넣어도 1x1 단위로 맞추기 위한 마법의 보정값
        float baseWidth = wallSprite.bounds.size.x;
        float baseHeight = wallSprite.bounds.size.y;

        // 스케일 계산 시 baseWidth/baseHeight로 나눠줍니다!
        topWall.position = new Vector2(center.x, PlayZoneMax.y + half);
        topWall.localScale = new Vector3(screenWidth / baseWidth, wallThickness / baseHeight, 1f);

        bottomWall.position = new Vector2(center.x, PlayZoneMin.y - half);
        bottomWall.localScale = new Vector3(screenWidth / baseWidth, wallThickness / baseHeight, 1f);

        leftWall.position = new Vector2(PlayZoneMin.x - half, center.y);
        leftWall.localScale = new Vector3(wallThickness / baseWidth, screenHeight / baseHeight, 1f);

        rightWall.position = new Vector2(PlayZoneMax.x + half, center.y);
        rightWall.localScale = new Vector3(wallThickness / baseWidth, screenHeight / baseHeight, 1f);
    }

    Transform CreateWall(string name)
    {
        GameObject wall = new GameObject(name);
        wall.transform.parent = root;
        wall.AddComponent<SpriteRenderer>().sprite = wallSprite;
        wall.GetComponent<SpriteRenderer>().color = wallColor;
        wall.AddComponent<BoxCollider2D>();
        wall.tag = "Wall";
        wall.layer = LayerMask.NameToLayer("Wall");
        return wall.transform;
    }


    //void OnDrawGizmos()
    //{
    //    if (mainCam == null) mainCam = Camera.main;
    //    if (mainCam == null) return;

    //    // 1. 계산용 기본 좌표 구하기 (에디터 실시간 반영용)
    //    Vector3 fMin = mainCam.ViewportToWorldPoint(new Vector3(0, bottomMargin, 10));
    //    Vector3 fMax = mainCam.ViewportToWorldPoint(new Vector3(1, 1 - topMargin, 10));

    //    // 2. FullZone 그리기 (화면 마진만 적용된 전체 틀)
    //    // 빨간색 점선 스타일로 그려서 PlayZone과 구분합니다.
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireCube((fMin + fMax) / 2f, fMax - fMin);

    //    // 3. PlayZone 좌표 계산 (벽 두께 반영)
    //    // 현재 코드 로직상 벽 두께만큼 안으로 들어온 위치
    //    Vector3 pMin = new Vector3(fMin.x + wallThickness, fMin.y + wallThickness, 0);
    //    Vector3 pMax = new Vector3(fMax.x - wallThickness, fMax.y - wallThickness, 0);

    //    // 4. PlayZone 그리기 (실제 공과 벽돌이 노는 영역)
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireCube((pMin + pMax) / 2f, pMax - pMin);

    //    //// 5. 파란색 격자 (PlayZone 내부에만 그리기)
    //    //Gizmos.color = new Color(0, 1, 1, 0.5f); // 약간 투명한 하늘색
    //    //for (int i = 0; i <= 10; i++)
    //    //{
    //    //    float t = i / 10f;
    //    //    // 가로선
    //    //    float y = Mathf.Lerp(pMin.y, pMax.y, t);
    //    //    Gizmos.DrawLine(new Vector3(pMin.x, y, 0), new Vector3(pMax.x, y, 0));
    //    //    // 세로선
    //    //    float x = Mathf.Lerp(pMin.x, pMax.x, t);
    //    //    Gizmos.DrawLine(new Vector3(x, pMin.y, 0), new Vector3(x, pMax.y, 0));
    //    //}
    //}

}

