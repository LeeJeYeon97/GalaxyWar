using UnityEngine;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameData")]
public class GameDataSO : ScriptableObject
{

    [Tooltip("게임 시작 전 카운트 다운 시간")]
    public int GameStartTime;

    [Header("Object Spawn Settings")]
    public float meteorSpawnInterval; // 생성 간격
    public float itemSpawnInterval; // 생성 간격
    public float bossSpawnInterval;  // 3분

    [Header("Pick Reload Count")]
    public int cardReloadCount;
    public int reviveCount;

    [Header("GameTestMode")]
    public bool expZero;
    public bool playerGod;
}