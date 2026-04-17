using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;


[Serializable]
public struct FireBulletStatData
{
    public float fireDamageValue;
    public float fireRemainTime;        // 화상 지속 시간
}

[CreateAssetMenu(fileName = "FireBulletData", menuName = "ScriptableObjects/BulletData/Fire")]
public class FireBulletStatDataSO : BulletStatDataSO // 상속!
{
    public FireBulletStatData fireStat;
    public override BaseBulletStat CreateRuntimeStat()
    {
        return new FireBulletStat();
    }
}