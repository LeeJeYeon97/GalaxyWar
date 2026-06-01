using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct BulletBounceLevelData
{
    public float countIncrease;  // 발사체 수 증가
    public float chanceIncrease; // 멀티샷 확률 증가
}

[CreateAssetMenu(fileName = "BulletBounceCountUpAbility", menuName = "ScriptableObjects/Ability/Player/BulletBounceCountUp")]
public class PlayerAllBulletBounceCountUpAbilityDataSO : PlayerAbilityDataSO
{
    public List<BulletBounceLevelData> increases = new List<BulletBounceLevelData>();
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

        foreach (var stat in Managers.Stat.bulletStatDict)
        {
            // 유도탄은 패스
            if(stat.Value.type == Define.BulletType.HomingBullet)
            {
                continue;
            }
            
            stat.Value.bounceCount.AddValue(increases[level].countIncrease);
        }
        targetStat.bounceChance.AddValue(increases[level].chanceIncrease);
        Managers.Event.PostEvent(Define.ActionEvent.BulletBounceCountUp, increases[level]);
    }
}

