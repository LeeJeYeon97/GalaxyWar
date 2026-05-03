using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


[CreateAssetMenu(fileName = "PlayerShieldAbility", menuName = "ScriptableObjects/Ability/Player/PlayerShield")]
public class PlayerShieldAbilityDataSO : PlayerAbilityDataSO
{
    public List<int> increases = new List<int>();
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

        int count = 0;
        // 플레이어의 쉴드 충전량 줄이기
        if(level == 0)
        {
            count = 1;
        }
        Managers.Game._player.UpgradeShield(increases[level], count);
    }
}

