using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


[CreateAssetMenu(fileName = "BulletSpeedUpAbility", menuName = "ScriptableObjects/Ability/Player/BulletSpeedUpAbility")]
public class PlayerAllBulletSpeedUpAbilityDataSO : PlayerAbilityDataSO
{
    public List<int> increases = new List<int>();
    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level <= 0 || level > increases.Count) return;

        foreach (var stat in Managers.Stat.bulletStatDict)
        {
            stat.Value.speed.AddValue(increases[level - 1]);
        }
    }
}

