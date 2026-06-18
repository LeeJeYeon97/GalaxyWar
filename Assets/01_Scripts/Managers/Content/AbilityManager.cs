using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class AbilityManager 
{
    // 이제 플레이어가 현재 보유한 능력에 대한 관리합니다. (나머지 정보는 SO에 다 있음)
    public Dictionary<AbilityType, int> _abilityLevels = new Dictionary<AbilityType, int>();
    public void Init()
    {
        // 적용중인 능력치 초기화
        _abilityLevels.Clear();
    }

    public List<AbilityDataSO> GetRandomAbility(int count = 3)
    {
        if (Managers.Data.AbilityDataDict.Count <= 0) return null;

        Debug.Log("GetRandomAbility 불림");
        // 후보군 필터링 (현재 레벨이 데이터의 MaxLevel보다 작은 것만)
        List<AbilityDataSO> candidates = new List<AbilityDataSO>();
        foreach (var data in Managers.Data.AbilityDataDict.Values)
        {
            // (주의) 무한으로 뽑을 대체 능력(체력 회복 등)은 이 일반 후보군에 안 들어가게 
            // 별도의 타입 조건으로 빼거나, 애초에 maxLevel을 999로 설정해 두는 것이 좋습니다.
            if (data.type == AbilityType.Passive_PlayerHeal)
                continue;

            // 1. 만렙 체크
            if (GetCurrentLevel(data.type) >= data.maxLevel) 
                continue;

            // 2. 선행 조건 체크 
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

        // 2. 일반 후보군에서 가능한 만큼 최대한 뽑기
        int drawCount = Mathf.Min(count, candidates.Count);
        for (int i = 0; i < drawCount; i++)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            selection.Add(candidates[randomIndex]);
            candidates.RemoveAt(randomIndex); // 중복 방지
        }

        //  3. 만약 카드를 다 못 채웠다면? (예: 3장을 뽑아야 하는데 1장만 뽑힌 경우)
        if (selection.Count < count)
        {
            // 대체로 띄울 데이터 가져오기 (SO 데이터 딕셔너리에서 안전하게 추출)
            Managers.Data.AbilityDataDict.TryGetValue(AbilityType.Passive_PlayerHeal, out AbilityDataSO fallbackHeal);
            //Managers.Data.AbilityDataDict.TryGetValue(AbilityType.Gold, out AbilityDataSO fallbackGold);

            int needed = count - selection.Count;
            for (int i = 0; i < needed; i++)
            {
                if(fallbackHeal != null)
                {
                    selection.Add(fallbackHeal);
                }
                //// 원하는 배치 로직으로 채워넣습니다. 
                //// 예: 힐 - 골드 - 힐 순서로 나오게 하거나, 전부 힐로 나오게 하기
                //if (i % 2 == 0 && fallbackHeal != null)
                //{
                //    selection.Add(fallbackHeal);
                //}
                //else if (fallbackHeal != null) // 골드가 없으면 그냥 다 힐로 채움
                //{
                //    selection.Add(fallbackHeal);
                //}
            }
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
    public int GetMaxLevel(AbilityType type)
    {
        if (Managers.Data.AbilityDataDict.TryGetValue(type, out var value))
            return value.maxLevel;
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

        int curLevel = _abilityLevels[type];
        // 케이스 A: 만약 선택한 카드가 '총알 능력(BulletAbility)' 이라면?
        if (selectedData is BulletAbilityDataSO bulletAbility)
        {
            BaseBulletStat targetBulletStat;
            if (bulletAbility.bulletType == BulletType.PierceBullet)
            {
                targetBulletStat = Managers.Stat.GetBulletStat(BulletType.NormalBullet);
            }
            else
            {
                targetBulletStat = Managers.Stat.GetBulletStat(bulletAbility.bulletType);
            }
             
            if (targetBulletStat != null)
            {
                // 총알 스탯을 던져주고 레벨업 시킴
                bulletAbility.ApplyLevelUp(curLevel, targetBulletStat);
                _abilityLevels[type]++;
            }
        }
        // 케이스 B: 만약 선택한 카드가 '플레이어 패시브(PlayerAbility)' 라면?
        else if (selectedData is PlayerAbilityDataSO playerAbility)
        {

            PlayerStat targetPlayerStat = Managers.Stat.playerStat;

            if (targetPlayerStat != null)
            {
                // 플레이어 스탯을 던져주고 레벨업 시킴
                playerAbility.ApplyLevelUp(curLevel, targetPlayerStat);
                _abilityLevels[type]++;
            }
        }
        else
        {
            Debug.LogWarning("알 수 없는 능력 타입입니다!");
        }
    }

}

