using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct SplitShotLevelData
{
    public float countIncrease;  // 발사체 수 증가
    public float chanceIncrease; // 멀티샷 확률 증가
    //public float angleChange;    // 퍼지는 각도 변경
}
[CreateAssetMenu(fileName = "SplitBulletAbility", menuName = "ScriptableObjects/Ability/Player/SplitBullet")]
public class PlayerSplitBulletAbilityDataSO : PlayerAbilityDataSO
{

    public List<SplitShotLevelData> splitIncreases = new List<SplitShotLevelData>();
    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level <= 0 || level > splitIncreases.Count) return;

        SplitShotLevelData data = splitIncreases[level - 1];

        // 플레이어 스탯에 멀티샷 관련 수치들만 딱딱 더해줍니다!
        targetStat.isMultiShotEnabled = true; // 멀티샷 활성화!
        targetStat.multiShotCount.AddValue(data.countIncrease);
        targetStat.multiShotChance.AddValue(data.chanceIncrease);
        //targetStat.multiShotAngle.AddValue(data.angleChange);

    }
}
