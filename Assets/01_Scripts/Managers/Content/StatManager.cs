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
    public Dictionary<BulletType, BaseBulletStat> bulletStatDict = new Dictionary<BulletType, BaseBulletStat>();
    [SerializeField]
    public Dictionary<MeteorType, MeteorStat> meteorStatDict = new Dictionary<MeteorType, MeteorStat>();
    [SerializeField]
    public Dictionary<BossType, BossStat> bossStatDict = new Dictionary<BossType, BossStat>();

    // 둔화 코루틴 상태를 기억할 변수들
    private Coroutine _playerSlowCoroutine;
    private bool _isSlowed = false;
    private float _currentSlowValue = 0f;
    public void Init()
    {
        // 플레이어 스탯
        playerStat = new PlayerStat();
        playerStat.SetStat(Managers.Data.playerStatData);

        // 2. [추가된 코드] 영구 업그레이드 수치를 기본 스탯 위에 덧바르기!
        Managers.Upgrade.ApplyPermanentUpgrades(playerStat);
        // 불릿
        foreach (var data in Managers.Data.BulletDataDict)
        {
            BaseBulletStat stat = data.Value.CreateRuntimeStat();
            stat.Init(data.Value);
            bulletStatDict.Add(data.Value.type, stat);
        }

        // 메테오 스탯
        foreach (var data in Managers.Data.MeteorStatDataDict)
        {
            MeteorStat stat = new MeteorStat();
            stat.Init(data.Value);
            meteorStatDict.Add(data.Value.Type, stat);
        }
        foreach (var data in Managers.Data.BossStatDataDict)
        {
            BossStat stat = new BossStat();
            stat.Init(data.Value);
            bossStatDict.Add(data.Value.Type, stat);
        }
    }
    public void Clear()
    {
        bulletStatDict.Clear();
        meteorStatDict.Clear();
        bossStatDict.Clear();
    }
    public BaseBulletStat GetRandomBulletStat()
    {
        // 1. 활성화된(Unlocked) 스탯들만 따로 모을 리스트를 직접 만듭니다.
        List<BaseBulletStat> activeStats = new List<BaseBulletStat>();
        int totalWeight = 0;

        // 2. 전체 딕셔너리를 돌면서 체크합니다.
        foreach (var stat in bulletStatDict.Values)
        {
            if (stat.curLevel >= 1 && stat.isReload == true)
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
    public BaseBulletStat GetBulletStat(BulletType type)
    {
        if (bulletStatDict.TryGetValue(type, out var stat))
        {
            return stat;
        }
        Debug.LogWarning($"{type.ToString()}에 해당하는 스탯이 없습니다!");
        return null;
    }
    public MeteorStat GetRandomSpawnMeteorStat()
    {
        // 딕셔너리가 비어있으면 null 반환
        if (meteorStatDict.Count <= 0) return null;

        // 1. 뽑을 수 있는(유효한) 스탯들만 모아둘 '빈 바구니(List)'를 준비합니다.
        List<MeteorStat> validStats = new List<MeteorStat>();

        Define.PhaseType currentPhase = Managers.Stage.CurrentPhase;

        // 2. 딕셔너리에 있는 모든 항목(Key-Value)을 하나씩 꺼내서 살펴봅니다.
        foreach (var stat in meteorStatDict.Values)
        {
            if(stat.isExclude == true)
            {
                continue;
            }

            // 현재 페이즈가 최소/최대 페이즈 범위를 벗어나면 제외
            if ((int)currentPhase > (int)stat.maxPhase || (int)currentPhase < (int)stat.minPhase)
            {
                continue;
            }

            validStats.Add(stat);
        }
        // 만약 다 걸러져서 바구니에 남은 게 하나도 없다면 null 반환
        if (validStats.Count == 0) return null;

        // 3. [핵심 추가] 가중치 기반 랜덤 뽑기 (룰렛 휠 방식)
        // 인스펙터나 데이터에 spawnWeight(가중치)가 있다면 사용하고, 없으면 기본값 1f로 처리합니다.
        float totalWeight = 0f;
        foreach (var stat in validStats)
        {
            totalWeight += stat.weight;
        }


        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        // 가중치를 누적해가며 어떤 메테오가 당첨되었는지 확인
        foreach (var stat in validStats)
        {
            currentWeight += stat.weight;
            if (randomValue <= currentWeight)
            {
                return stat;
            }
        }

        // 예외 오차 방지용 안전 장치
        return validStats[0];

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

    public BossStat GetRandomBossStat()
    {
        // 딕셔너리가 비어있으면 null 반환
        if (bossStatDict.Count <= 0) return null;

        // 1. 뽑을 수 있는(유효한) 스탯들만 모아둘 '빈 바구니(List)'를 준비합니다.
        List<BossStat> validStats = new List<BossStat>();


        // 2. 딕셔너리에 있는 모든 항목(Key-Value)을 하나씩 꺼내서 살펴봅니다.
        foreach (var stat in bossStatDict.Values)
        {
            
            validStats.Add(stat);
        }
        // 만약 다 걸러져서 바구니에 남은 게 하나도 없다면 null 반환
        if (validStats.Count == 0) return null;

        // 5. 안전하게 모인 바구니 안에서 랜덤으로 하나를 뽑습니다.
        int randIdx = UnityEngine.Random.Range(0, validStats.Count);

        return validStats[randIdx];

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
