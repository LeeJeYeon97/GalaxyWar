using System;
using UnityEngine;
using UnityEngine.Localization;


[Serializable]
public struct ExplosionBulletStatData
{
    [Header("폭발탄 전용 증가량")]
    public float explosionRange;    // 기본 폭발 범위
    public float explosionDamageValue;   // 폭발 데미지
}


[CreateAssetMenu(fileName = "ExplosionBulletData", menuName = "ScriptableObjects/BulletData/Explosion")]
public class ExplosionBulletStatDataSO : BulletStatDataSO
{
    [Header("Explosion Stat Settings")]
    public ExplosionBulletStatData explosionStat;   // 폭발 데미지

    public override BaseBulletStat CreateRuntimeStat()
    {
        return new ExplosionBulletStat();
    }
}
