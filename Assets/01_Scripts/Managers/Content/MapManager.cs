using UnityEngine;


public class MapManager
{
    [Header("Play Zone Settings (%)")]
    [Range(0, 0.5f)] public float topMargin = 0f;    // 상단 비움
    [Range(0, 0.5f)] public float bottomMargin = 0f; // 하단 비움

    [Header("Wall Settings")]
    public float wallThickness = 0.03f;       // 시각적인 벽의 두께
    public float colliderThickness = 50f;     // 터널링 방지용 물리 두께
    private Camera mainCam;

    public Vector2 PlayZoneMin { get; private set; }
    public Vector2 PlayZoneMax { get; private set; }

    Vector3 fullzoneMin;
    Vector3 fullzoneMax;

    private Transform topWall, bottomWall, leftWall, rightWall;

    public Transform root;
    // 프리팹 원본의 스프라이트 사이즈를 저장할 변수
    private float baseWidth = 1f;
    private float baseHeight = 1f;

    private bool isInit = false;
    public void Init()
    {
        // 1. 기존 오브젝트가 있다면 삭제 (로비 갔다 돌아왔을 때 중복 방지)
        GameObject oldMap = GameObject.Find("@Map");
        if (oldMap != null) Managers.Resource.Destroy(oldMap);

        // 2. 카메라 재할당
        mainCam = Camera.main;

        root = new GameObject("@Map").transform;

            
        // 1. 처음 한 번만 벽을 생성하여 변수에 할당
        GenerateInitialWalls();

        // 2. 현재 카메라 사이즈에 맞춰 초기 배치
        UpdateMap();
        isInit = true;
    }

    void GenerateInitialWalls()
    {
        topWall = Managers.Resource.Instantiate("Object/Wall").transform;
        bottomWall = Managers.Resource.Instantiate("Object/Wall").transform;
        leftWall = Managers.Resource.Instantiate("Object/Wall").transform;
        rightWall = Managers.Resource.Instantiate("Object/Wall").transform;

        topWall.parent = root;
        bottomWall.parent = root;
        leftWall.parent = root;
        rightWall.parent = root;

        // [핵심 추가] 하단 벽은 메테오가 닿았을 때 체력을 깎고 사라져야 하므로 Trigger로 설정
        BoxCollider2D bottomCol = bottomWall.GetComponent<BoxCollider2D>();
        if (bottomCol != null)
        {
            bottomCol.isTrigger = true;
        }

        SpriteRenderer sr = topWall.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            baseWidth = sr.sprite.bounds.size.x;
            baseHeight = sr.sprite.bounds.size.y;
        }
    }
    public void OnUpdate()
    {
        if(isInit == true)
        {
            //  Managers에서 호출해주므로, 여기서 직접 UpdateMap을 실행합니다.
            // LateUpdate에서 부르고 싶다면 Managers에서 LateUpdate에 배치하세요.
            UpdateMap();
        }
    }
    public void Clear()
    {
        // 3. 루트 오브젝트 완전 삭제
        if (root != null)
        {
            Managers.Resource.Destroy(root.gameObject);
            root = null;
        }
        isInit = false;
    }
    public void UpdateMap()
    {
        // 카메라가 파괴되었거나 아직 없으면 튕기지 않게 방어 로직 추가
        if (mainCam == null) return;

        CalculatePlayZone();
        RepositionWalls();
    }

    void CalculatePlayZone()
    {
        float worldHeight = mainCam.orthographicSize * 2f;
        float worldWidth = worldHeight * (9f / 19.5f);

        Vector3 camPos = mainCam.transform.position;

        float leftX = camPos.x - (worldWidth / 2f);
        float rightX = camPos.x + (worldWidth / 2f);

        float bottomY = camPos.y - mainCam.orthographicSize + (worldHeight * bottomMargin);
        float topY = camPos.y + mainCam.orthographicSize - (worldHeight * topMargin);

        fullzoneMin = new Vector3(leftX, bottomY, 0);
        fullzoneMax = new Vector3(rightX, topY, 0);

        PlayZoneMin = new Vector2(fullzoneMin.x + wallThickness, fullzoneMin.y + wallThickness);
        PlayZoneMax = new Vector2(fullzoneMax.x - wallThickness, fullzoneMax.y - wallThickness);
    }

    void RepositionWalls()
    {
        float screenWidth = fullzoneMax.x - fullzoneMin.x;
        float screenHeight = fullzoneMax.y - fullzoneMin.y;
        Vector2 center = (PlayZoneMin + PlayZoneMax) / 2f;
        float half = wallThickness / 2f;

        topWall.position = new Vector2(center.x, PlayZoneMax.y + half);
        topWall.localScale = new Vector3(screenWidth / baseWidth, wallThickness / baseHeight, 1f);

        bottomWall.position = new Vector2(center.x, PlayZoneMin.y - half);
        bottomWall.localScale = new Vector3(screenWidth / baseWidth, wallThickness / baseHeight, 1f);

        leftWall.position = new Vector2(PlayZoneMin.x - half, center.y);
        leftWall.localScale = new Vector3(wallThickness / baseWidth, screenHeight / baseHeight, 1f);

        rightWall.position = new Vector2(PlayZoneMax.x + half, center.y);
        rightWall.localScale = new Vector3(wallThickness / baseWidth, screenHeight / baseHeight, 1f);

        ApplyThickCollider(topWall, isHorizontal: true, direction: 1f);
        ApplyThickCollider(bottomWall, isHorizontal: true, direction: -1f);
        ApplyThickCollider(leftWall, isHorizontal: false, direction: -1f);
        ApplyThickCollider(rightWall, isHorizontal: false, direction: 1f);
    }

    void ApplyThickCollider(Transform wall, bool isHorizontal, float direction)
    {
        BoxCollider2D col = wall.GetComponent<BoxCollider2D>();
        if (col == null) return;

        if (isHorizontal)
        {
            float localThickness = colliderThickness / wall.localScale.y;
            col.size = new Vector2(baseWidth, localThickness);
            col.offset = new Vector2(0, direction * (localThickness - baseHeight) / 2f);
        }
        else
        {
            float localThickness = colliderThickness / wall.localScale.x;
            col.size = new Vector2(localThickness, baseHeight);
            col.offset = new Vector2(direction * (localThickness - baseWidth) / 2f, 0);
        }
    }

}

