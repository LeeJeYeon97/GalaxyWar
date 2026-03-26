using System;

using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public struct HomingBulletStatData
{
    public float homingRange;
}
[CreateAssetMenu(fileName = "HomingBulletData", menuName = "ScriptableObjects/BulletData/Homing")]
public class HomingBulletStatDataSO : BulletStatDataSO
{
    public HomingBulletStatData homingBulletStat;

    public override BaseBulletStat CreateRuntimeStat()
    {
        return new HomingBulletStat();
    }
}