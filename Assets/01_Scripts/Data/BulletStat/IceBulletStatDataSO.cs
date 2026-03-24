using System;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public struct IceBulletStatData
{
    [Header("얼음탄 전용 증가량")]
    public float slowValue;
    public float slowTime;
    public float freezeChance;
    public float freezeTime;
}
[CreateAssetMenu(fileName = "IceBulletData", menuName = "ScriptableObjects/BulletData/Ice")]
public class IceBulletStatDataSO : BulletStatDataSO
{
    public IceBulletStatData iceBulletStat;

    public override BaseBulletStat CreateRuntimeStat()
    {
        return new IceBulletStat();
    }
}
