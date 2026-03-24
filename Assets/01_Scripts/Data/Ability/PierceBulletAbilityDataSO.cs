using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


[CreateAssetMenu(fileName = "PierceBulletAbilityData", menuName = "ScriptableObjects/Ability/PierceBullet")]
public class PierceBulletAbilityDataSO : BulletAbilityDataSO
{
    [Header("관통탄 스탯 증가량")]
    public List<PierceBulletStatData> stats = new List<PierceBulletStatData>();

    // 뼈대(Base)에서 시킨 스탯 적용 함수를 내 입맛에 맞게 구현!
    public override void ApplyLevelUp(int level, BaseBulletStat targetStat)
    {
        if (level <= 0 || level > stats.Count) return;

        // 내 현재 레벨 데이터 꺼내기
        PierceBulletStatData data = stats[level - 1];

        if (targetStat is PierceBulletStat stat)
        {
            stat.pierceCount.AddValue(data.pierceCount);
            stat.pierceDamageDecreaseValue.AddValue(data.pierceDamageDecreaseValue);
        }
    }
}