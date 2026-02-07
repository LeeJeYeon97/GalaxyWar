using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[Serializable]
public class StatManager
{
    // ∫“∏¥µÈ Ω∫≈»
    [SerializeField]
    public Dictionary<BulletType, BulletStat> bulletStatDict = new Dictionary<BulletType, BulletStat>();

    public void Init()
    {
        
        // ∫“∏¥µÈ Ω∫≈»
        foreach(var data in Managers.Data.BulletDataDict)
        {
            BulletStat stat = new BulletStat();
            stat.SettingStat(data.Value);
            bulletStatDict.Add(data.Value.type, stat);
        }
    }

    public BulletStat GetBulletStat(BulletType type)
    {
        if (bulletStatDict.TryGetValue(type, out var stat))
        {
            return stat;
        }
        Debug.LogWarning($"{type.ToString()}ø° «ÿ¥Á«œ¥¬ Ω∫≈»¿Ã æ¯Ω¿¥œ¥Ÿ!");
        return null;
    }
}
