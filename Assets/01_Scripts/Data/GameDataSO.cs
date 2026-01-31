using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameData")]
public class GameDataSO : ScriptableObject
{
    [Header("Exp Gain Settings")]
    public float baseExpGain = 10f;          // 몬스터 한 마리당 기본 경험치
    public float expGainIncreasePerLevel = 2f; // 레벨당 추가 경험치 획득량

    [Header("Level Up Settings (Required Exp)")]
    public float baseMaxExp = 50f;          // 1레벨에서 필요한 최대 경험치
    public float maxExpMultiplier = 1.2f;    // 레벨업 시 필요 경험치 상승률 (예: 1.2배씩 증가)

    public float MaxStuckTime = 5f;
}