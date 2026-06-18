using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


[Serializable]
public struct mineStatData
{
    public float minMineDelay;
    public float mineDamageValue;
    public float mineExplodeRadius;
}

[CreateAssetMenu(fileName = "MineAbilityData", menuName = "ScriptableObjects/Ability/MineAbilityData")]
public class MineAbilityDataSO : PlayerAbilityDataSO
{
    public List<mineStatData> values = new List<mineStatData>();

    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel < 0)
        {
            return null;
        }

        return new object[] { values[0] };
    }

    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level < 0 || level > values.Count) return;

        targetStat.isMineEnabled = true;
        targetStat.mineDropDelay.AddValue(values[level].minMineDelay);
        targetStat.mineExplodeRadius.AddValue(values[level].mineExplodeRadius);
        targetStat.mineDamageValue.AddValue(values[level].mineDamageValue);

    }
}

