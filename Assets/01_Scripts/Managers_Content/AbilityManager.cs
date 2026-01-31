using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public struct AbilityInfo
{
    public AbilityType _type; // 능력 종류

    public int maxLevel;
    public int curLevel;

    [Header("선행 조건 설정")]
    // 이 값이 Unknown이면 선행 조건이 없는 것임
    public AbilityType _requiredAbility;

    public float values;
}
public class AbilityManager 
{
    // 현재 플레이어가 보유한 능력 (UI 표시용)
    private Dictionary<AbilityType, AbilityInfo> _abilities = new Dictionary<AbilityType, AbilityInfo>();

    public void Init()
    {
        // 적용중인 능력치 초기화
        _abilities.Clear();
    }

    public List<AbilityDataSO> GetRandomAbility(int count = 3)
    {
        if (Managers.Data.AbilityDataDict.Count <= 0) return null;

        // 후보군 필터링 (현재 레벨이 데이터의 MaxLevel보다 작은 것만)
        List<AbilityDataSO> candidates = new List<AbilityDataSO>();
        foreach (var data in Managers.Data.AbilityDataDict.Values)
        {
            // 1. 만렙 체크
            if (GetCurrentLevel(data.type) >= data.maxLevel) 
                continue;

            // 2. ★ 선행 조건 체크 ★
            if (data._requiredAbility != Define.AbilityType.Unknown)
            {
                // 요구하는 선행 능력의 레벨이 0이라면(보유하지 않았다면) 후보에서 탈락
                if (GetCurrentLevel(data._requiredAbility) <= 0)
                    continue;
            }

            // 후보군 등록
            candidates.Add(data);
        }

        if(candidates.Count < count)
        {
            count = candidates.Count;
        }

        List<AbilityDataSO> selection = new List<AbilityDataSO>();

        for (int i = 0; i < count; i++)
        {
            if (candidates.Count == 0) break;

            int randomIndex = Random.Range(0, candidates.Count);
            selection.Add(candidates[randomIndex]);
            candidates.RemoveAt(randomIndex);
        }

        return selection;
    }
    public void ApplyAbility(AbilityDataSO data)
    {
        if (data == null) return;

        if (_abilities.TryGetValue(data.type, out AbilityInfo info))
        {
            // --- [이미 보유한 경우: 레벨업 및 수치 추가] ---
            info.curLevel++;

            // 데이터에 정의된 해당 레벨의 증가치를 현재 합산 리스트에 반영
            if (data.values.Count >= info.curLevel)
            {
                float value = data.values[info.curLevel - 1];
                // 합산된 총 수치를 저장
                info.values += value;

                Debug.Log($"[{data.abilityname}] 강화! Lv.{info.curLevel}, 총합: {info.values}");
            }

            // ★ 중요: 구조체는 값 타입이므로 수정한 뒤 다시 딕셔너리에 넣어줘야 합니다.
            _abilities[data.type] = info;
        }
        else
        {
            // --- [신규 획득인 경우: 초기화] ---
            AbilityInfo newInfo = new AbilityInfo
            {
                _type = data.type,
                maxLevel = data.maxLevel,
                curLevel = 1,
                _requiredAbility = data._requiredAbility,
                values = data.values[0]
            };
            
            _abilities.Add(data.type, newInfo);
            
            Debug.Log($"[{data.abilityname}] 최초 습득!");
        }

        // 2. ★ StatManager의 BulletStat에 수치 반영 ★
        float increaseValue = data.values[GetCurrentLevel(data.type) - 1];

        switch (data.type)
        {
            case AbilityType.Unknown:
                break;
            case AbilityType.UpgradeBaseBulletDamage:
                break;
            case AbilityType.UpgradeBaseBulletHp:
                break;
            case AbilityType.UpgradeBaseBulletCount:
                break;
            case AbilityType.ActivateSplitBullet:
                break;
            case AbilityType.UpgradeSplitBulletDamage:
                break;
            case AbilityType.UpgradeSplitBulletCount:
                break;
            case AbilityType.UpgradeSplitBulletChance:
                break;
            case AbilityType.ActivateExplosionBullet:
                BulletStat stat = Managers.Stat.GetBulletStat(BulletType.ExplosionBullet);
                stat.isActivated = true;
                break;
            case AbilityType.UpgradeExplosionDamage:
                break;
            case AbilityType.UpgradeExplosionRange:
                break;
            case AbilityType.UpgradeExplosionChance:
                break;
        }
    }
    // 특정 능력의 현재 레벨 반환
    public int GetCurrentLevel(AbilityType type)
    {
        if (_abilities.TryGetValue(type, out AbilityInfo info))
            return info.curLevel;
        return 0;
    }

}

