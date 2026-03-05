using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[Serializable]
public class StatManager
{
    // 스탯들
    [SerializeField]
    public PlayerStat playerStat;
    [SerializeField]
    public Dictionary<BulletType, BulletStat> bulletStatDict = new Dictionary<BulletType, BulletStat>();
    [SerializeField]
    public Dictionary<MeteorType, MeteorStat> meteorStatDict = new Dictionary<MeteorType, MeteorStat>();

    public void Init()
    {
        // 플레이어 스탯
        playerStat = new PlayerStat();
        playerStat.SetStat(Managers.Data.playerStatData);

        // 불릿들 스탯
        foreach (var data in Managers.Data.BulletDataDict)
        {
            BulletStat stat = new BulletStat();
            stat.SettingStat(data.Value);
            bulletStatDict.Add(data.Value.type, stat);
        }

        // 메테오 스탯
        foreach (var data in Managers.Data.MeteorStatDataDict)
        {
            MeteorStat stat = new MeteorStat();
            stat.Init(data.Value);
            meteorStatDict.Add(data.Value.Type, stat);
        }
    }
    public void Clear()
    {
        bulletStatDict.Clear();
        meteorStatDict.Clear();
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

    public void ApplyAbility(AbilityDataSO data, float value)
    {
        if (data == null) return;

        switch (data.type)
        {
            case AbilityType.Unknown:
                break;
            // --- 플레이어 및 공통 유틸리티 (1~7) ---
            case AbilityType.UpgradePlayerHp:
                playerStat.maxHp.AddValue(value);
                break;
            case AbilityType.UpgradePlayerSpeed:
                playerStat.speed.AddValue(value);
                break;
            case AbilityType.UpgradeReloadCount:
                playerStat.reloadCount.AddValue(value);
                break;
            case AbilityType.UpgradeBulletBounceCount:
                foreach (var bulletStat in bulletStatDict.Values)
                {
                    bulletStat.bounceCount.AddValue(value);
                }
                break;
            case AbilityType.UpgradeBulletSpeed:
                foreach (var bulletStat in bulletStatDict.Values)
                {
                    bulletStat.speed.AddValue(value);
                }
                break;
            case AbilityType.UpgradeReloadTime:
                playerStat.reloadTime.AddMultiplier(value);
                break;
            case AbilityType.UpgradeShotTime:
                playerStat.shotTime.AddMultiplier(value);
                break;

            // --- 기본탄 (10) ---
            case AbilityType.UpgradeBaseBulletDamage:
                break;

            // --- 분열탄 (20~23) ---
            case AbilityType.ActivateSplitBullet:
                GetBulletStat(BulletType.SplitBullet).isActivated = true;
                break;
            case AbilityType.UpgradeSplitBulletDamage:
                GetBulletStat(BulletType.SplitBullet).damage.AddValue(value);
                break;
            case AbilityType.UpgradeSplitBulletCount:
                GetBulletStat(BulletType.SplitBullet).splitCount.AddValue(value);
                break;
            case AbilityType.UpgradeSplitBulletChance:
                GetBulletStat(BulletType.SplitBullet).chance.AddValue(value);
                break;

            // --- 폭발탄 (30~33) ---
            case AbilityType.ActivateExplosionBullet:
                GetBulletStat(BulletType.ExplosionBullet).isActivated = true;
                break;
            case AbilityType.UpgradeExplosionDamage:
                GetBulletStat(BulletType.ExplosionBullet).damage.AddValue(value);
                break;
            case AbilityType.UpgradeExplosionRange:
                GetBulletStat(BulletType.ExplosionBullet).explosionRadius.AddValue(value);
                break;
            case AbilityType.UpgradeExplosionChance:
                GetBulletStat(BulletType.ExplosionBullet).chance.AddValue(value);
                break;

            // --- 번개탄 (40~43) ---
            case AbilityType.ActivateLightningBullet:
                GetBulletStat(BulletType.LightningBullet).isActivated = true;
                break;
            case AbilityType.UpgradeLigthningCount:
                GetBulletStat(BulletType.LightningBullet).lightningCount.AddValue(value);
                break;
            case AbilityType.UpgradeLigthningDamage:
                GetBulletStat(BulletType.LightningBullet).damage.AddValue(value);
                break;
            case AbilityType.UpgradeLightningRange:
                GetBulletStat(BulletType.LightningBullet).lightningRange.AddValue(value);
                break;
            case AbilityType.UpgradeLightningChance:
                GetBulletStat(BulletType.LightningBullet).chance.AddValue(value);
                break;

            // --- 관통탄 (50~52) ---
            case AbilityType.ActivatePierceBullet:
                break;
            case AbilityType.UpgradePierceCount:
                break;
            case AbilityType.UpgradePierceDamage:
                break;

            // --- 특수 모드 (60) ---
            case AbilityType.ActivateBurstMode:
                Managers.Event.PostEvent(ActionEvent.EnableBurstMode);
                break;

            default:
                Debug.LogWarning($"정의되지 않은 AbilityType입니다: {data.type}");
                break;
        }
    }
    
}
