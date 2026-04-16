using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;



[CreateAssetMenu(fileName = "LightningBulletAbilityData", menuName = "ScriptableObjects/Ability/LightningBullet")]
public class LightningBulletAbilityDataSO : BulletAbilityDataSO
{
    [Header("번개탄 스탯 증가량")]
    public List<LightningBulletStatData> stats = new List<LightningBulletStatData>();

    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel <= 0 || nextLevel > stats.Count)
        {
            return null;
        }
        // 폭발탄은 수치가 2개니까 2개만 배열로 묶어서 줍니다.
        return new object[] { stats[nextLevel].lightningDamageValue, stats[nextLevel].lightningRange, stats[nextLevel].lightningCount };
    }

    // 뼈대(Base)에서 시킨 스탯 적용 함수를 내 입맛에 맞게 구현!
    public override void ApplyLevelUp(int level, BaseBulletStat targetStat)
    {
        if (level < 0 || level > stats.Count) return;

        // 내 현재 레벨 데이터 꺼내기
        LightningBulletStatData data = stats[level];

        if (targetStat is LightningBulletStat stat)
        {
            stat.lightningRange.AddValue(data.lightningRange);
            stat.lightningDamageValue.AddValue(data.lightningDamageValue);
            stat.lightningCount.AddValue(data.lightningCount);
        }
        base.ApplyLevelUp(level, targetStat);
    }
}