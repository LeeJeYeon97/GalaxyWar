using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;


[Serializable]
public struct PierceBulletStatData
{
    public int pierceCount;
    public int pierceDamageDecreaseValue;
}
[CreateAssetMenu(fileName = "PierceBulletData", menuName = "ScriptableObjects/BulletData/Pierce")]
public class PierceBulletStatDataSO : BulletStatDataSO
{
    public PierceBulletStatData pierceBulletStat;

    public override BaseBulletStat CreateRuntimeStat()
    {
        return new PierceBulletStat();
    }
}
