using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameData")]
public class GameDataSO : ScriptableObject
{
    [Header("Exp Gain Settings")]
    public float baseExpGain = 10f;          // 몬스터 한 마리당 기본 경험치
    public float expGainIncreasePerLevel = 2f; // 레벨당 추가 경험치 획득량

    [Header("Level Up Settings (Required Exp)")]
    public float baseMaxExp;          // 1레벨에서 필요한 최대 경험치

    [Tooltip("게임 시작 전 카운트 다운 시간")]
    public int GameStartTime;

    [Header("Object Spawn Settings")]
    public float meteorSpawnInterval = 5f; // 생성 간격
    public float itemSpawnInterval = 60f; // 생성 간격
    public float bossSpawnInterval = 180f;  // 3분
}