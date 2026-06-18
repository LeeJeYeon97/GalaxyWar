using UnityEngine;
using UnityEngine.UI;

public class InfiniteBackGround : MonoBehaviour
{
    [Header("배경 타일 프리팹 (SpriteRenderer 필수)")]
    public GameObject bgPrefab;

    private Transform _playerTransform;
    private Transform[] _tiles = new Transform[9]; // 3x3 = 총 9개의 타일 풀링

    private float _tileSizeX;
    private float _tileSizeY;

    private void Start()
    {
        // 1. 플레이어 찾기
        _playerTransform = Managers.Game._player.transform;

        // 2. 맵 초기 세팅
        InitBackground();
    }

    private void InitBackground()
    {
        // 프리팹의 실제 스프라이트 크기를 자동으로 계산해서 가져옵니다! (수동 입력 방지)
        SpriteRenderer sr = bgPrefab.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("[InfiniteMap] 배경 프리팹에 SpriteRenderer가 없습니다!");
            return;
        }

        _tileSizeX = sr.bounds.size.x;
        _tileSizeY = sr.bounds.size.y;

        int index = 0;
        // 플레이어를 중심으로 3x3 그리드 형태로 9개의 타일을 찍어냅니다.
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2 spawnPos = new Vector2(_playerTransform.position.x + (x * _tileSizeX),
                                               _playerTransform.position.y + (y * _tileSizeY));

                // 타일을 생성하고 이 매니저의 자식으로 둡니다.
                GameObject tile = Instantiate(bgPrefab, spawnPos, Quaternion.identity, transform);
                _tiles[index] = tile.transform;
                index++;
            }
        }
    }

    private void LateUpdate()
    {
        if (_playerTransform == null) return;

        Vector2 playerPos = _playerTransform.position;

        // 9개의 타일을 매 프레임 검사합니다.
        for (int i = 0; i < _tiles.Length; i++)
        {
            Transform tile = _tiles[i];

            // 플레이어와 현재 타일의 거리 차이 (X축, Y축 각각 계산)
            float diffX = playerPos.x - tile.position.x;
            float diffY = playerPos.y - tile.position.y;

            //  [핵심] 타일이 화면 밖으로 완전히 벗어났다면? (타일 크기의 1.5배 이상 멀어짐)
            // 타일을 반대편 끝(진행 방향의 제일 앞쪽)으로 3칸만큼 텔레포트 시킵니다!

            // 가로축 재배치
            if (Mathf.Abs(diffX) >= _tileSizeX * 1.5f)
            {
                float dirX = diffX > 0 ? 1 : -1;
                tile.Translate(Vector3.right * dirX * _tileSizeX * 3f);
            }

            // 세로축 재배치
            if (Mathf.Abs(diffY) >= _tileSizeY * 1.5f)
            {
                float dirY = diffY > 0 ? 1 : -1;
                tile.Translate(Vector3.up * dirY * _tileSizeY * 3f);
            }
        }
    }
}
