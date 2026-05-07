using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


//[Serializable]
//public struct ExplosionBulletStatData
//{
//    public float explosionRange;         // 기본 폭발 범위
//    public float explosionDamageValue;   // 폭발 데미지
//}

[CreateAssetMenu(fileName = "BurstBulletData", menuName = "ScriptableObjects/BulletData/BurstBullet")]
public class BurstBulletStatDataSO : BulletStatDataSO
{
    public override BaseBulletStat CreateRuntimeStat()
    {
        return new BurstBulletStat();
    }
}



