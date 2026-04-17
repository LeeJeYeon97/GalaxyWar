using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "ReloadTimeDecreaseAbility", menuName = "ScriptableObjects/Ability/Player/ReloadTimeDecrease")]
public class PlayerReloadTimeDecreaseAbilityDataSO : PlayerAbilityDataSO
{
    public List<float> reloadTimeDecreases = new List<float>();

    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel < 0 || nextLevel > reloadTimeDecreases.Count)
        {
            return null;
        }
        // 폭발탄은 수치가 2개니까 2개만 배열로 묶어서 줍니다.
        return new object[] { reloadTimeDecreases[nextLevel] };
    }
    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level <0 || level > reloadTimeDecreases.Count) return;

        // 타겟(플레이어)의 스피드 스탯에 바로 더해줍니다!
        float amount = reloadTimeDecreases[level];
        targetStat.reloadTime.SubValue(amount);

    }
}

