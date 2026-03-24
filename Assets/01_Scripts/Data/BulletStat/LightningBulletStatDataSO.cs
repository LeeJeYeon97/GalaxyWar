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
    
    [Header("얼음탄 전용 증가량")]
    public float lightningDamageValue;
    public float lightningRange;
    public int lightningCount;

    public GameObject ligthningChainObject;
}

[CreateAssetMenu(fileName = "LightningBulletData", menuName = "ScriptableObjects/BulletData/Lightning")]
public class LightningBulletStatDataSO : BulletStatDataSO
{
    public LightningBulletStatData lightningStat;
    public override BaseBulletStat CreateRuntimeStat()
    {
        return new LightningBulletStat();
    }
}
