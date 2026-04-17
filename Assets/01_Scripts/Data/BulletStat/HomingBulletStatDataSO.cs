using System;

using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public struct HomingBulletStatData
{
    public float homingShotDelay;
}
[CreateAssetMenu(fileName = "HomingBulletData", menuName = "ScriptableObjects/BulletData/Homing")]
public class HomingBulletStatDataSO : BulletStatDataSO
{
    public HomingBulletStatData homingBulletStat;
    public float trunSpeed;

    public override BaseBulletStat CreateRuntimeStat()
    {
        return new HomingBulletStat();
    }
}