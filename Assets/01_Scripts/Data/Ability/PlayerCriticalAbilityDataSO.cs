using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct CriticalData
{
    public float CriticalChance;
    public float CriticalDamageRate;
}

[CreateAssetMenu(fileName = "PlayerCritical", menuName = "ScriptableObjects/Ability/Player/PlayerCritical")]
public class PlayerCriticalAbilityDataSO : PlayerAbilityDataSO
{
    public List<CriticalData> levels = new List<CriticalData>();
    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel < 0 || nextLevel > levels.Count)
        {
            return null;
        }
        // 폭발탄은 수치가 2개니까 2개만 배열로 묶어서 줍니다.
        return new object[] { levels[nextLevel].CriticalChance, levels[nextLevel].CriticalDamageRate };
    }

    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level < 0 || level > levels.Count) return;

        targetStat.criticalChance.AddValue(levels[level].CriticalChance);
        targetStat.criticalDamageRate.AddValue(levels[level].CriticalDamageRate);
    }
}

