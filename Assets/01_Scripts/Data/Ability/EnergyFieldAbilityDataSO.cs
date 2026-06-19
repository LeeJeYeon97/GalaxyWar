using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct EnergyFieldStatData
{
    public float damageValue;
    public float damageInterval; // 0.5초마다 데미지
    public float radius;
}

[CreateAssetMenu(fileName = "EnergyFieldAbilityData", menuName = "ScriptableObjects/Ability/EnergyFieldAbility")]
public class EnergyFieldAbilityDataSO : PlayerAbilityDataSO
{

    public List<EnergyFieldStatData> increases = new List<EnergyFieldStatData>();

    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel < 0 || nextLevel > increases.Count)
        {
            return null;
        }
        // 폭발탄은 수치가 2개니까 2개만 배열로 묶어서 줍니다.
        return new object[] { increases[nextLevel] };
    }

    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level < 0 || level > increases.Count) return;

        
        Managers.Game._player.Combat.ActivateEnergyField(increases[level]);
        
    }
}
