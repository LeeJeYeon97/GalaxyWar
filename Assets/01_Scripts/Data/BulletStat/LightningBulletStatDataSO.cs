using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;


[Serializable]
public struct LightningBulletStatData
{
    public float lightningDamageValue;
    public float lightningRange;
    public int lightningCount;
}

[CreateAssetMenu(fileName = "LightningBulletData", menuName = "ScriptableObjects/BulletData/Lightning")]
public class LightningBulletStatDataSO : BulletStatDataSO
{
    public LightningBulletStatData lightningStat;
    public GameObject lightningChain;

    public override BaseBulletStat CreateRuntimeStat()
    {
        return new LightningBulletStat();
    }
}
