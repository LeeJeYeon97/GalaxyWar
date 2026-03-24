using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "NormalBulletData", menuName = "ScriptableObjects/BulletData/Normal")]
public class NormalBulletStatDataSO : BulletStatDataSO
{
    public override BaseBulletStat CreateRuntimeStat()
    {
        return new NormalBulletStat();
    }
}