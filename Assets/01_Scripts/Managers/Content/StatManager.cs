using System;
using System.Collections.Generic;
using System.Linq;
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

    // 둔화 코루틴 상태를 기억할 변수들
    private Coroutine _playerSlowCoroutine;
    private bool _isSlowed = false;
    private float _currentSlowValue = 0f;
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
        // 딕셔너리가 비어있으면 null 반환
        if (meteorStatDict.Count <= 0) return null;

        // 1. 뽑을 수 있는(유효한) 스탯들만 모아둘 '빈 바구니(List)'를 준비합니다.
        List<MeteorStat> validStats = new List<MeteorStat>();

        Define.PhaseType currentPhase = Managers.Game.currentPhase;

        // 2. 딕셔너리에 있는 모든 항목(Key-Value)을 하나씩 꺼내서 살펴봅니다.
        foreach (var stat in meteorStatDict.Values)
        {
            if(stat.isExclude == true)
            {
                continue;
            }

            if ((int)currentPhase < (int)stat.spawnPhase) continue;

            validStats.Add(stat);
        }
        // 만약 다 걸러져서 바구니에 남은 게 하나도 없다면 null 반환
        if (validStats.Count == 0) return null;

        // 5. 안전하게 모인 바구니 안에서 랜덤으로 하나를 뽑습니다.
        int randIdx = UnityEngine.Random.Range(0, validStats.Count);

        return validStats[randIdx];

    }
    public MeteorStat GetMeteorStat(MeteorType type)
    {
        if (meteorStatDict.TryGetValue(type, out var stat))
        {
            return stat;
        }
        Debug.LogWarning($"{type.ToString()}에 해당하는 스탯이 없습니다!");
        return null;
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
                playerStat.reloadTime.SubMultiplier(value);
                break;
            case AbilityType.UpgradeShotTime:
                playerStat.shotTime.SubMultiplier(value);
                break;
            case AbilityType.ActivatePlayerShield:
                break;

            // --- 기본탄 (10) ---
            case AbilityType.UpgradeBaseBulletDamage:
                GetBulletStat(BulletType.NormalBullet).damage.AddValue(value);
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

    public void ApplyPlayerDebuff(DebuffType type, float value, float time)
    {
        switch (type)
        {
            case DebuffType.Slow:
                // 이미 둔화 타이머가 돌아가고 있다면 취소 (시간 갱신용)
                if (_playerSlowCoroutine != null)
                {
                    Managers.Coroutine.StopCoroutine(_playerSlowCoroutine);
                }

                // 코루틴 헬퍼에게 타이머를 대신 돌려달라고 명령!
                _playerSlowCoroutine = Managers.Coroutine.StartCoroutine(CoSlowDown(value, time));
                break;
        }
    }

    // 실제 시간의 흐름과 스탯 증감을 통제하는 핵심 코루틴
    private System.Collections.IEnumerator CoSlowDown(float value, float time)
    {
        // 1. 아직 안 느려진 상태라면 배율 깎기
        if (!_isSlowed)
        {
            _isSlowed = true;
            _currentSlowValue = value;
            playerStat.speed.SubMultiplier(_currentSlowValue);
        }

        // 2. 지정된 시간(0.2초) 동안 대기
        yield return new WaitForSeconds(time);

        // 3. 시간이 무사히 다 지났다면 원상 복구
        playerStat.speed.AddMultiplier(_currentSlowValue);

        // 상태 초기화
        _isSlowed = false;
        _playerSlowCoroutine = null;
    }
}
