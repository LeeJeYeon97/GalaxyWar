using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public abstract class PlayerAbilityDataSO : AbilityDataSO
{
    public abstract void ApplyLevelUp(int level, PlayerStat targetStat);
}

