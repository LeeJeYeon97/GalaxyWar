using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class MeteorSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1.5f; // 생성 간격
   
    private Coroutine _spawnCoroutine;

    public void Init()
    {
        StartSpawn();
    }
    
    public void StartSpawn()
    {
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
        _spawnCoroutine = StartCoroutine(CoSpawnMeteor());
    }
    public void StopSpawn()
    {
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
    }

    IEnumerator CoSpawnMeteor()
    {
        // 게임 상태가 Playing인 동안에만 무한 반복
        while (Managers.Game.currentGameState == GameState.Playing)
        {
            yield return new WaitForSeconds(spawnInterval);

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
                case 0: // 위쪽 외곽 (X는 랜덤, Y는 Max + offset)
                    spawnPos = new Vector3(Random.Range(minX, maxX), maxY + offset, 0);
                    break;
                case 1: // 아래쪽 외곽 (X는 랜덤, Y는 Min - offset)
                    spawnPos = new Vector3(Random.Range(minX, maxX), minY - offset, 0);
                    break;
                case 2: // 왼쪽 외곽 (X는 Min - offset, Y는 랜덤)
                    spawnPos = new Vector3(minX - offset, Random.Range(minY, maxY), 0);
                    break;
                case 3: // 오른쪽 외곽 (X는 Max + offset, Y는 랜덤)
                    spawnPos = new Vector3(maxX + offset, Random.Range(minY, maxY), 0);
                    break;
            }
            // 2. 풀링 매니저에서 운석 꺼내기
            // (주의: PoolingManager에 "Meteor"라는 이름으로 프리팹이 등록되어 있어야 함)
            MeteorController meteor = Managers.Pool.Get<MeteorController>(Define.Pool.Meteor);

            if (meteor != null)
            {
                // 3. 운석 초기화 (위치 설정 및 이동 시작)
                meteor.Init(spawnPos);
            }
        }
    }
}
