using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct PierceBulletStatData
{
    public int pierceCount;
    public float pierceDamageDecreaseValue;
}

[CreateAssetMenu(fileName = "NormalBulletData", menuName = "ScriptableObjects/BulletData/Normal")]
public class NormalBulletStatDataSO : BulletStatDataSO
{
    public PierceBulletStatData pierceBulletStat;
    public override BaseBulletStat CreateRuntimeStat()
    {
        return new NormalBulletStat();
    }
}