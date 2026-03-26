using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletBounceCountUpAbility", menuName = "ScriptableObjects/Ability/Player/BulletBounceCountUp")]
public class PlayerAllBulletBounceCountUpAbilityDataSO : PlayerAbilityDataSO
{
    public List<int> increases = new List<int>();
    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level <= 0 || level > increases.Count) return;

        foreach (var stat in Managers.Stat.bulletStatDict)
        {
            stat.Value.bounceCount.AddValue(increases[level - 1]);
        }

        Managers.Event.PostEvent(Define.ActionEvent.BulletBounceCountUp, increases[level - 1]);
    }
}

