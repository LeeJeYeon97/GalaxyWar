using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class PlayerBurstModeAblityDataSO : PlayerAbilityDataSO
{
    public List<float> levels = new List<float>();
    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel < 0 || nextLevel > levels.Count)
        {
            return null;
        }
        // 폭발탄은 수치가 2개니까 2개만 배열로 묶어서 줍니다.
        return new object[] { levels[nextLevel] };
    }

    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level < 0 || level > levels.Count) return;

        targetStat.enableBurst = true; // 버스트모드 활성화!
        targetStat.maxBurstFullChargeTime.SubValue(levels[level]);
    }
}

