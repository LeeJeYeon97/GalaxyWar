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
    // 특정 능력의 현재 레벨 반환
    public int GetCurrentLevel(AbilityType type)
    {
        if (_abilityLevels.TryGetValue(type, out int level))
            return level;
        return 0;
    }
    public void ApplyAbility(AbilityDataSO selectedData)
    {
        if (selectedData == null) return;

        AbilityType type = selectedData.type;

        // 1. 레벨업 처리 (명부에 없으면 0으로 시작, 그리고 +1)
        if (!_abilityLevels.ContainsKey(type))
        {
            _abilityLevels[type] = 0;
        }

        _abilityLevels[type]++;
        int currentLevel = _abilityLevels[type];

        // ==========================================
        // 2. 마법의 다형성 분기 처리 (is 키워드 활용)
        // ==========================================

        // 케이스 A: 만약 선택한 카드가 '총알 능력(BulletAbility)' 이라면?
        if (selectedData is BulletAbilityDataSO bulletAbility)
        {
            
            // TODO: 플레이어가 현재 들고 있는 '해당 총알의 런타임 스탯'을 가져와야 합니다!
            // (예: 플레이어 스크립트나 WeaponManager에서 가져오는 함수 호출)
            BaseBulletStat targetBulletStat = Managers.Stat.GetBulletStat(bulletAbility.bulletType);

            if (targetBulletStat != null)
            {
                // 총알 스탯을 던져주고 레벨업 시킴
                bulletAbility.ApplyLevelUp(currentLevel, targetBulletStat);
                targetBulletStat.curLevel = currentLevel;
                Debug.Log($"{selectedData.type} (총알) 능력이 {currentLevel}레벨로 적용되었습니다!");
            }
        }
        // 케이스 B: 만약 선택한 카드가 '플레이어 패시브(PlayerAbility)' 라면?
        else if (selectedData is PlayerAbilityDataSO playerAbility)
        {
            // TODO: 플레이어 본체의 런타임 스탯을 가져와야 합니다!
            PlayerStat targetPlayerStat = Managers.Stat.playerStat;

            if (targetPlayerStat != null)
            {
                // 플레이어 스탯을 던져주고 레벨업 시킴
                playerAbility.ApplyLevelUp(currentLevel, targetPlayerStat);
                Debug.Log($"{selectedData.type} (플레이어) 능력이 {currentLevel}레벨로 적용되었습니다!");
            }
        }
        else
        {
            Debug.LogWarning("알 수 없는 능력 타입입니다!");
        }
    }

}

