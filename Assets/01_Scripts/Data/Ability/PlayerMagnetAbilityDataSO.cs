using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


[CreateAssetMenu(fileName = "MagnetAbility", menuName = "ScriptableObjects/Ability/Player/MagnetAbility")]
public class PlayerMagnetAbilityDataSO : PlayerAbilityDataSO
{
    // 구조체 필요 없이 그냥 float 리스트면 충분합니다!
    public List<float> values = new List<float>();
    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel < 0)
        {
            return null;
        }

        return new object[] { values[0]};
    }
    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level < 0 || level > values.Count) return;

        targetStat.itemGetRange.AddMultiplier(values[level]);
        Managers.Game._player.Magnetic.UpdateMagneticRange();
    }
}

