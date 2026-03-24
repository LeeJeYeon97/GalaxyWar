using System;
using UnityEngine;

[Serializable]
public struct PlayerStatData
{
    public float speed;

    public float maxHp;
    public float maxDefence;
    public float maxBurstGuage;
    public float maxBurstFullChargeTime;

    public float reloadCount;
    public float reloadTime;

    public float shotRange;
    public float shotTime;

    public float hitCooldown;       // ÇÇ°Ý µô·¹ÀÌ

    [Header("¸ÖÆ¼¼¦°ü·Ã")]
    public bool isMultiShotEnabled; // ¸ÖÆ¼¼¦(ºÐ¿­) ´É·Â È¹µæ ¿©ºÎ
    public float multiShotCount;
    public float multiShotChance;
    public float multiShotAngle;
}
[CreateAssetMenu(fileName = "PlayerStatData", menuName = "ScriptableObjects/PlayerStatData")]
public class PlayerStatDataSO : ScriptableObject
{
    public PlayerStatData statData;
}
