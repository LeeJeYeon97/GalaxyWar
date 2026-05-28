using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Define;

public class Spawner : MonoBehaviour
{
    private Coroutine _meteorSpawnCoroutine;
    private Coroutine _itemSpawnCoroutine;

    public void StartSpawn()
    {
        if (_meteorSpawnCoroutine != null) StopCoroutine(_meteorSpawnCoroutine);
        _meteorSpawnCoroutine = StartCoroutine(CoSpawnMeteor());

        //if (_itemSpawnCoroutine != null) StopCoroutine(_itemSpawnCoroutine);
        //_itemSpawnCoroutine = StartCoroutine(CoSpawnItem());
    }
    public void StopSpawn()
    {
        if (_meteorSpawnCoroutine != null) StopCoroutine(_meteorSpawnCoroutine);

        if (_itemSpawnCoroutine != null) StopCoroutine(_itemSpawnCoroutine);
    }

    IEnumerator CoSpawnMeteor()
    {
        // StopSpawn()이 호출되기 전까지는 코루틴이 스스로 죽지 않고 계속 살아있게 합니다.
        while (Managers.Game.currentGameState != GameState.GameOver)
        {
            //  2. 방어막 추가: 일시정지 중이라면 멍때리면서 다음 프레임으로 넘깁니다.
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                yield return null;
                continue;
            }

            float currentInterval = Managers.Stage.CurrentSpawnDelay;

            // 3. 마법의 타이머 적용! (알아서 Pause 상태면 시간이 안 흐릅니다)
            yield return new WaitForGameTime(currentInterval);

            //  4. 대기가 끝났는데 그 찰나의 순간에 게임이 멈췄을 수도 있으니 한 번 더 체크!
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                continue;
            }

            // 1. 어느 방향(상, 하, 좌, 우)에서 생성할지 결정
            int side = Random.Range(0, 4); // 0: 위, 1: 아래, 2: 왼쪽, 3: 오른쪽
            Vector3 spawnPos = Vector3.zero;

            // 외곽에서 얼마나 떨어뜨릴지 (여유분)
            float offset = 0.5f;

            float minX = Managers.Map.PlayZoneMin.x;
            float maxX = Managers.Map.PlayZoneMax.x;
            float minY = Managers.Map.PlayZoneMin.y;
            float maxY = Managers.Map.PlayZoneMax.y;

            switch (side)
            {
                case 0: // 위쪽 외곽
                    spawnPos = new Vector3(Random.Range(minX, maxX), maxY + offset, 0);
                    break;
                case 1: // 아래쪽 외곽
                    spawnPos = new Vector3(Random.Range(minX, maxX), minY - offset, 0);
                    break;
                case 2: // 왼쪽 외곽
                    spawnPos = new Vector3(minX - offset, Random.Range(minY, maxY), 0);
                    break;
                case 3: // 오른쪽 외곽
                    spawnPos = new Vector3(maxX + offset, Random.Range(minY, maxY), 0);
                    break;
            }

            // 2. 풀링 매니저에서 운석 꺼내기
            MeteorStat stat = Managers.Stat.GetRandomSpawnMeteorStat();
            MeteorController meteor = Managers.Resource.Instantiate(stat.originalPrefabs).GetComponent<MeteorController>();

            if (meteor != null)
            {
                // 3. 운석 초기화 (위치 설정 및 이동 시작)
                meteor.Init(spawnPos, stat);
            }
        }
    }
    public void BossSpawn(BossStat stat)
    {
        if (stat == null) return;

        // 1. 어느 방향(상, 하, 좌, 우)에서 생성할지 결정
        int side = Random.Range(0, 4); // 0: 위, 1: 아래, 2: 왼쪽, 3: 오른쪽
        Vector3 spawnPos = Vector3.zero;

        //  보스는 덩치가 크므로 메테오(0.5f)보다 여유 공간을 더 줍니다! (예: 1.5f)
        float offset = 1.5f;

        float minX = Managers.Map.PlayZoneMin.x;
        float maxX = Managers.Map.PlayZoneMax.x;
        float minY = Managers.Map.PlayZoneMin.y;
        float maxY = Managers.Map.PlayZoneMax.y;

        switch (side)
        {
            case 0: // 위쪽 외곽
                spawnPos = new Vector3(Random.Range(minX, maxX), maxY + offset, 0);
                break;
            case 1: // 아래쪽 외곽
                spawnPos = new Vector3(Random.Range(minX, maxX), minY - offset, 0);
                break;
            case 2: // 왼쪽 외곽
                spawnPos = new Vector3(minX - offset, Random.Range(minY, maxY), 0);
                break;
            case 3: // 오른쪽 외곽
                spawnPos = new Vector3(maxX + offset, Random.Range(minY, maxY), 0);
                break;
        }

        // 2. 보스 프리팹 생성 (스폰 위치 적용)
        // ResourceManager의 Instantiate를 쓰면 풀링이 적용되어 있으면 풀링에서, 아니면 새로 생성됩니다.
        GameObject bossGo = Managers.Resource.Instantiate(stat.originalPrefab);

        // 3. 보스 컴포넌트 찾기 및 초기화 세팅
        BossController boss = bossGo.GetComponent<BossController>();
        if (boss != null)
        {
            boss.Init(stat, spawnPos, Managers.Game._player.gameObject);
            
            Debug.Log($"보스가 {side}번 방향({spawnPos})에서 스폰되었습니다!");
        }
    }
    public void SpawnDropItem(Vector3 position, ItemType type, int customValue = 0)
    {
        if(Managers.Data.ItemDataList.TryGetValue(type, out var itemData))
        {
            if(itemData.isDrop == false)
            {
                return;
            }
        }
        else
        {
            return;
        }

        GameObject go = Managers.Resource.Instantiate(itemData.originalPrefab);
        ItemController item = go.GetComponent<ItemController>();

        if(item == null)
        {
            return;
        }

        item.Init(position, itemData, customValue);
    }

    //IEnumerator CoSpawnItem()
    //{
    //    float itemSpawnInterval = Managers.Data.GameData.itemSpawnInterval;
    //    // 게임 상태가 Playing인 동안에만 무한 반복
    //    while (Managers.Game.currentGameState != GameState.GameOver)
    //     {
    //         if (Managers.Game.currentGameState == GameState.Pause)
    //         {
    //             yield return null;
    //             continue;
    //         }
    //
    //         yield return new WaitForGameTime(itemSpawnInterval);
    //
    //         if (Managers.Game.currentGameState == GameState.Pause)
    //         {
    //             continue;
    //         }

    //        // 1. 어느 방향(상, 하, 좌, 우)에서 생성할지 결정
    //        int side = Random.Range(0, 4); // 0: 위, 1: 아래, 2: 왼쪽, 3: 오른쪽
    //        Vector3 spawnPos = Vector3.zero;

    //        // 외곽에서 얼마나 떨어뜨릴지 (여유분)
    //        float offset = 0.5f;

    //        float minX = Managers.Map.PlayZoneMin.x;
    //        float maxX = Managers.Map.PlayZoneMax.x;
    //        float minY = Managers.Map.PlayZoneMin.y;
    //        float maxY = Managers.Map.PlayZoneMax.y;

    //        switch (side)
    //        {
    //            case 0: // 위쪽 외곽 (X는 랜덤, Y는 Max + offset)
    //                spawnPos = new Vector3(Random.Range(minX, maxX), maxY + offset, 0);
    //                break;
    //            case 1: // 아래쪽 외곽 (X는 랜덤, Y는 Min - offset)
    //                spawnPos = new Vector3(Random.Range(minX, maxX), minY - offset, 0);
    //                break;
    //            case 2: // 왼쪽 외곽 (X는 Min - offset, Y는 랜덤)
    //                spawnPos = new Vector3(minX - offset, Random.Range(minY, maxY), 0);
    //                break;
    //            case 3: // 오른쪽 외곽 (X는 Max + offset, Y는 랜덤)
    //                spawnPos = new Vector3(maxX + offset, Random.Range(minY, maxY), 0);
    //                break;
    //        }
    //        // 2. 풀링 매니저에서 운석 꺼내기
    //        // (주의: PoolingManager에 "Meteor"라는 이름으로 프리팹이 등록되어 있어야 함)
    //        ItemController item = Managers.Pool.Get<ItemController>(Define.Pool.Item);

    //        if (item != null)
    //        {
    //            // 3. 아이템 초기화 (위치 설정 및 이동 시작)
    //            item.Init(spawnPos);
    //        }
    //    }
    //}
}
