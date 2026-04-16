
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ReloadCountUpAbility", menuName = "ScriptableObjects/Ability/Player/ReloadCountUp")]
public class PlayerReloadCountUpAbilityDataSO : PlayerAbilityDataSO
{
    public List<float> reloadCountIncreases = new List<float>();

    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel <= 0 || nextLevel > reloadCountIncreases.Count)
        {
            return null;
        }
        // 폭발탄은 수치가 2개니까 2개만 배열로 묶어서 줍니다.
        return new object[] { reloadCountIncreases[nextLevel] };
    }
    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level <= 0 || level > reloadCountIncreases.Count) return;

        // 타겟(플레이어)의 스피드 스탯에 바로 더해줍니다!
        float amount = reloadCountIncreases[level - 1];
        targetStat.reloadCount.AddValue(amount);

    }
}

