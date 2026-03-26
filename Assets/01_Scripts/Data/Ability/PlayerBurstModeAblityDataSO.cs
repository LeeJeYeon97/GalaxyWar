using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class PlayerBurstModeAblityDataSO : PlayerAbilityDataSO
{
    public List<float> levels = new List<float>();
    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level <= 0 || level > levels.Count) return;

        targetStat.enableBurst = true; // 버스트모드 활성화!
        targetStat.maxBurstFullChargeTime.SubValue(levels[level - 1]);
    }
}

