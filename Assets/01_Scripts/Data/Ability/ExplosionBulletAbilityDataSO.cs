using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "ExplosionBulletAbilityData", menuName = "ScriptableObjects/Ability/ExplosionBullet")]
public class ExplosionBulletAbilityDataSO : BulletAbilityDataSO
{
    [Header("폭발탄 스탯 증가량")]
    public List<ExplosionBulletStatData> stats = new List<ExplosionBulletStatData>();

    public override void ApplyLevelUp(int level, BaseBulletStat targetStat)
    {
        if (level <= 0 || level > stats.Count) return;

        // 내 현재 레벨 데이터 꺼내기
        ExplosionBulletStatData data = stats[level - 1];

        if (targetStat is ExplosionBulletStat stat)
        {
            stat.explosionRange.AddValue(data.explosionRange);
            stat.explosionDamage.AddValue(data.explosionDamageValue);

        }
    }
}