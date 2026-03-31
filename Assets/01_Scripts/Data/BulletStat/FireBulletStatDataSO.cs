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
    public float fireRemainTime;        // 장판 지속시간
    public float fireTickTime;          // 화상 데미지 틱 시간
    public float fireZoneRadius;        // 장판 범위
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