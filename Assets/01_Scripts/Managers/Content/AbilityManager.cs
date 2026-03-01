using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class AbilityManager 
{
    // 이제 플레이어가 현재 보유한 능력에 대한 관리합니다. (나머지 정보는 SO에 다 있음)
    private Dictionary<AbilityType, int> _abilityLevels = new Dictionary<AbilityType, int>();
    public void Init()
    {
        // 적용중인 능력치 초기화
        _abilityLevels.Clear();
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
        int nextLevel = GetCurrentLevel(data.type) + 1;
        _abilityLevels[data.type] = nextLevel;
        float value = data.GetValue(nextLevel);

        // 데이터 수정 권한을 가진 StatManager에게 요청 (본인이 직접 안 함)
        Managers.Stat.ApplyAbility(data, value);

        // 특수 기능(함수 실행)이 필요한 경우에만 예외적으로 처리
        //if (data.targetType == AbilityTargetType.Special)
        //{
        //    HandleSpecialLogic(data.type, value);
        //}
    }
    // 특정 능력의 현재 레벨 반환
    public int GetCurrentLevel(AbilityType type)
    {
        if (_abilityLevels.TryGetValue(type, out int level))
            return level;
        return 0;
    }
    
}

