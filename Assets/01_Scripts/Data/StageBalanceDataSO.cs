using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageBalanceData", menuName = "ScriptableObjects/StageBalanceData")]
public class StageBalanceDataSO : ScriptableObject
{
    [Header("1. 기본 스탯 (1스테이지 기준)")]
    public float baseSpawnDelay = 2.0f;     // 초기 스폰 간격 (초)
    public float minSpawnDelay = 0.3f;      // 아무리 빨라져도 0.3초 밑으로는 안 내려감

    [Header("매 스테이지마다 스폰간격 감소량")]
    public float spawnDelayDecrease = 0.02f;// 매 스테이지마다 스폰 간격 0.02초씩 감소

    [Header("2. 난이도 증가 공식 (스테이지당 증가량)")]
    public float hpGrowthPerStage = 10f;    // (선형) 매 스테이지마다 체력 20씩 증가
    public float hpMultiplierPerStage = 1.02f; // (복리) 매 스테이지마다 체력 2%씩 곱연산 증

    [Header("매 스테이지마다 속도 증가량")]
    public float speedGrowthPerStage = 0.05f;// 매 스테이지마다 속도 0.05씩 증가

    [Header("4. 10분 생존 웨이브 배율 (Wave)")]
    public List<WaveMultiplier> waves;      // 시간대별로 쏟아지는 물량/체력 배율

    [Header("보스 스테이지 지정 데이터 리스트")]
    public int bossStageInterval = 5;          // 몇 스테이지마다 보스가 나올 것인가? (예: 5)
}

[System.Serializable]
public class WaveMultiplier
{
    [Header("페이즈 및 시간")]
    public Define.PhaseType phaseType;      // 이 웨이브가 무슨 페이즈인지 지정!
    public float waveStartTime;             // 발동 시간 (0초, 180초, 360초...)
    public float waveHpRate = 1f;           // 이 웨이브의 체력 배율 (후반부는 2배 등)
    public float waveSpawnDelayRate = 1f;   // 이 웨이브의 스폰 속도 배율 (후반부는 0.5배 등)
}
