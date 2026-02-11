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
    public Dictionary<MeteorType, MeteorStat> meteorStatDict = new Dictionary<MeteorType, MeteorStat>();

    public void Init()
    {
        // ∫“∏¥µÈ Ω∫≈»
        foreach(var data in Managers.Data.BulletDataDict)
        {
            BulletStat stat = new BulletStat();
            stat.SettingStat(data.Value);
            bulletStatDict.Add(data.Value.type, stat);
        }

        foreach (var data in Managers.Data.MeteorStatDataDict)
        {
            MeteorStat stat = new MeteorStat();
            stat.Init(data.Value);
            meteorStatDict.Add(data.Value.Type, stat);
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
    public MeteorStat GetRandomMeteorStat()
    {
        if (meteorStatDict.Count <= 0) return null;

        int maxCount = meteorStatDict.Count;
        int randIdx = UnityEngine.Random.Range(0, maxCount);

        return meteorStatDict[(MeteorType)randIdx];
        
    }
}
