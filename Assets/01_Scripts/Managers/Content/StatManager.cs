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
    public Dictionary<MeteorType, MeteorStat> meteorStatDict = new Dictionary<MeteorType, MeteorStat>();

    public void Init()
    {
        // 불릿들 스탯
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
    public BulletStat GetRandomBulletStat()
    {
        // 1. 활성화된(Unlocked) 스탯들만 따로 모을 리스트를 직접 만듭니다.
        List<BulletStat> activeStats = new List<BulletStat>();
        int totalWeight = 0;

        // 2. 전체 딕셔너리를 돌면서 체크합니다. (LINQ의 Where + Sum 역할)
        foreach (var stat in bulletStatDict.Values)
        {
            if (stat.isActivated)
            {
                activeStats.Add(stat);
                totalWeight += (int)stat.chance.TotalValue; // 합계도 동시에 구합니다.
            }
        }

        // 예외 처리: 활성화된 탄환이 없으면 기본탄 반환
        if (activeStats.Count == 0 || totalWeight == 0)
        {
            return GetBulletStat(BulletType.NormalBullet);
        }

        // 3. 당첨 번호 뽑기
        int pivot = UnityEngine.Random.Range(0, totalWeight);
        int currentWeight = 0;

        // 4. 어떤 구간에 당첨됐는지 순회하며 확인
        for (int i = 0; i < activeStats.Count; i++)
        {
            currentWeight += (int)activeStats[i].chance.TotalValue;

            if (pivot < currentWeight)
            {
                // 당첨! 해당 타입에 맞는 데이터를 가져옵니다.
                return activeStats[i];
            }
        }

        return GetBulletStat(BulletType.NormalBullet);
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
    public MeteorStat GetRandomMeteorStat()
    {
        if (meteorStatDict.Count <= 0) return null;

        int maxCount = meteorStatDict.Count;
        int randIdx = UnityEngine.Random.Range(0, maxCount);

        return meteorStatDict[(MeteorType)randIdx];
        
    }
}
