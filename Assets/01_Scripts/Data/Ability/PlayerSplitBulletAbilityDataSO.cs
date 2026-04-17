using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

[Serializable]
public struct SplitShotLevelData
{
    public float countIncrease;  // 발사체 수 증가
    public float chanceIncrease; // 멀티샷 확률 증가
}
[CreateAssetMenu(fileName = "SplitBulletAbility", menuName = "ScriptableObjects/Ability/Player/SplitBullet")]
public class PlayerSplitBulletAbilityDataSO : PlayerAbilityDataSO
{

    public List<SplitShotLevelData> splitIncreases = new List<SplitShotLevelData>();
    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel < 0 || nextLevel > splitIncreases.Count)
        {
            return null;
        }
        // 폭발탄은 수치가 2개니까 2개만 배열로 묶어서 줍니다.
        return new object[] { splitIncreases[nextLevel].chanceIncrease, splitIncreases[nextLevel].countIncrease };
    }
    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level < 0 || level > splitIncreases.Count) return;

        SplitShotLevelData data = splitIncreases[level];

        // 플레이어 스탯에 멀티샷 관련 수치들만 딱딱 더해줍니다!
        targetStat.isMultiShotEnabled = true; // 멀티샷 활성화!
        targetStat.multiShotCount.AddValue(data.countIncrease);
        targetStat.multiShotChance.AddValue(data.chanceIncrease);

    }
}
