using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


[CreateAssetMenu(fileName = "FireBulletAbilityData", menuName = "ScriptableObjects/Ability/FireBullet")]
public class FireBulletAbilityDataSO : BulletAbilityDataSO
{
    [Header("화염탄 스탯 증가량")]
    public List<FireBulletStatData> stats = new List<FireBulletStatData>();

    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel <= 0 || nextLevel > stats.Count)
        {
            return null;
        }

        return new object[] { stats[nextLevel].fireDamageValue, stats[nextLevel].fireRemainTime, stats[nextLevel].fireZoneDestroyTime };
    }

    // 뼈대(Base)에서 시킨 스탯 적용 함수를 내 입맛에 맞게 구현!
    public override void ApplyLevelUp(int level, BaseBulletStat targetStat)
    {
        if (level < 0 || level > stats.Count) return;

        // 내 현재 레벨 데이터 꺼내기
        FireBulletStatData data = stats[level];

        if (targetStat is FireBulletStat stat)
        {
            stat.fireDamageValue.AddValue(data.fireDamageValue);
            stat.fireRemainTime.AddValue(data.fireRemainTime);

        }
        base.ApplyLevelUp(level, targetStat);
    }
}