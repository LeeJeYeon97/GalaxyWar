using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[Serializable]
public class StatManager
{
    // 불릿들 스탯
    [SerializeField]
    public Dictionary<BulletType, BulletStat> bulletStatDict = new Dictionary<BulletType, BulletStat>();

    public void Init()
    {
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
        Debug.LogWarning($"{type.ToString()}에 해당하는 스탯이 없습니다!");
        return null;
    }
}
