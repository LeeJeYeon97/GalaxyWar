using System;
using UnityEngine;

[Serializable]
public struct PlayerStatData
{
    public float speed;
    public float damage;
    public float maxHp;
    public float maxDefenceCount;
    
    public float reloadCount;
    public float reloadTime;

    public float shotRange;
    public float shotTime;

    public float criticalChance;
    public float criticalDamageRate;

    public float itemGetRange;

    public float hitCooldown;       // 피격 딜레이

    public float shieldChargeTime;
    public float maxDefenceGuage;

    public float bounceChance;

    [Header("버스트모드관련")]
    public bool isBurstModeEnabled;
    public float maxBurstGuage;
    public float maxBurstFullChargeTime;

    [Header("멀티샷관련")]
    public bool isMultiShotEnabled; // 멀티샷(분열) 능력 획득 여부
    public float multiShotCount;
    public float multiShotChance;
    public float multiShotAngle;

    [Header("유도탄 관련")]
    public bool isHomingShotEnabled;
    public float homingShotDelay;
    public float homingRange;
}
[CreateAssetMenu(fileName = "PlayerStatData", menuName = "ScriptableObjects/PlayerStatData")]
public class PlayerStatDataSO : ScriptableObject
{
    public PlayerStatData statData;
}
