using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

public class UpgradeManager
{
    // 유저의 현재 업그레이드 상태를 저장하는 딕셔너리 (Key: 업그레이드 ID, Value: 현재 레벨)
    private Dictionary<UpgradeType, int> _upgradeLevels = new Dictionary<UpgradeType, int>();


    public void InitializeServerData(Dictionary<string, int> serverUpgradeLevels)
    {
        _upgradeLevels.Clear(); // 덮어씌우기 전 안전하게 비우기

        if (serverUpgradeLevels == null || serverUpgradeLevels.Count == 0)
        {
            Debug.Log("[UpgradeManager] 서버에 저장된 업그레이드 데이터가 없습니다. (초기 상태)");
            return;
        }

        // 서버에서 온 데이터를 하나씩 꺼내서 Enum으로 변환 후 로컬 딕셔너리에 저장
        foreach (var kvp in serverUpgradeLevels)
        {
            // string을 Define.UpgradeType Enum으로 안전하게 변환
            if (System.Enum.TryParse(kvp.Key, out Define.UpgradeType type))
            {
                _upgradeLevels[type] = kvp.Value;
            }
            else
            {
                Debug.LogWarning($"[UpgradeManager] 알 수 없는 업그레이드 타입입니다: {kvp.Key}");
            }
        }

        Debug.Log($"[UpgradeManager] 서버로부터 {_upgradeLevels.Count}개의 영구 업그레이드 데이터를 성공적으로 불러왔습니다!");
    }

    // 1. 특정 업그레이드의 현재 레벨 가져오기
    public int GetUpgradeLevel(Define.UpgradeType type)
    {
        if (_upgradeLevels.TryGetValue(type, out int level))
        {
            return level;
        }
        return 0; // 저장된 데이터가 없으면 0레벨 반환
    }

    // 2. 레벨 강제 세팅 (세이브 데이터 로드용)
    public void SetUpgradeLevel(Define.UpgradeType type, int level)
    {
        if (_upgradeLevels.ContainsKey(type))
        {
            _upgradeLevels[type] = level;
        }
        else
        {
            _upgradeLevels.Add(type, level);
        }
    }

    // 3.  레벨업 비즈니스 로직 (UI가 아닌 매니저에서 검증하고 처리하는 것이 안전합니다)
    // 반환형을 async Task<bool> 로 변경하고, 함수 이름도 Async를 붙여 명확하게 합니다.
    public async Task<bool> TryLevelUpAsync(UpgradeDataSO data)
    {
        int currentLevel = GetUpgradeLevel(data.type);

        // 만렙 체크
        if (currentLevel >= data.MaxLevel) return false;

        var nextInfo = data.levelInfos[currentLevel];

        // 재화 체크 (로컬에서 1차 검증)
        if (Managers.PlayerEconomy.Gold < nextInfo.cost) return false;

        // 유저에게 "처리 중..."임을 알리기 위해 로딩 팝업을 띄웁니다.
        var popup = Managers.UI.ShowPopupUI<UI_LoadingPopup>();

        try
        {
            // 2. 서버(Cloud Code)에 코인 소모 요청
            var spendCurrency = await Managers.PlayerEconomy.SpendGoldAsync(nextInfo.cost);

            //  응답을 받았으므로 가장 먼저 로딩 팝업부터 닫아줍니다.
            if (popup != null) Managers.UI.ClosePopupUI(popup);

            if (spendCurrency == true)
            {
                Debug.Log("코인 소모 성공! 업그레이드.");

                int newLevel = currentLevel + 1;

                // 레벨 상승
                SetUpgradeLevel(data.type, newLevel);

                // 서버에 변경된 업그레이드 데이터만 쏙 저장!
                bool saveSuccess = await Managers.PlayerData.SaveUpgradeDataAsync(data.type, newLevel);

                if (!saveSuccess)
                {
                    // 치명적인 통신 에러로 골드만 까이고 업그레이드 저장이 안 되었을 때의 방어 코드
                    // (일단 경고 로그만 남겨둡니다. 필요시 여기서 롤백 로직을 추가할 수 있습니다.)
                    Debug.LogWarning("서버에 업그레이드 내역을 저장하는 데 실패했습니다!");
                }

                return true;
            }
            else
            {
                // 서버 결과가 실패(코인 부족 등)인 경우
                Debug.Log("서버에서 재화 소모가 거절되었습니다.");
                return false; //  실패했을 때도 false를 반환해야 에러가 나지 않습니다.
            }
        }
        catch (System.Exception e)
        {
            //  통신 에러가 났을 때도 로딩 팝업이 무한정 떠있지 않게 닫아주어야 합니다!
            if (popup != null) Managers.UI.ClosePopupUI(popup);

            Debug.LogError($"코인 사용 중 서버 에러 발생: {e.Message}");
            Debug.Log("네트워크 통신에 실패했습니다.");

            return false; //  에러 발생 시에도 false 반환
        }
    }
    public void ApplyPermanentUpgrades(PlayerStat playerStat)
    {
        // DataManager에 있는 모든 업그레이드 종류를 순회합니다.
        foreach (var upgradeData in Managers.Data.UpgradeDataDict.Values)
        {
            // 현재 이 업그레이드의 레벨을 가져옵니다.
            int currentLevel = GetUpgradeLevel(upgradeData.type);

            // 1레벨 이상일 때만 스탯을 적용합니다.
            if (currentLevel > 0)
            {
                // [핵심 변경점] 1레벨부터 현재 달성한 레벨까지의 증가치를 모두 합산합니다.
                float totalBonusValue = 0f;
                for (int i = 0; i < currentLevel; i++)
                {
                    totalBonusValue += upgradeData.levelInfos[i].statValue;
                }

                // Enum 타입에 맞춰서 playerStat의 알맞은 변수에 '누적된 총합'을 더해줍니다.
                switch (upgradeData.type)
                {
                    case Define.UpgradeType.HP:
                        // 예시: playerStat의 최대 체력에 누적 보너스를 더함
                        playerStat.maxHp.AddValue(totalBonusValue);
                        break;
                    case Define.UpgradeType.Damage:
                        // 예시: playerStat의 공격력에 누적 보너스를 더함 (변수명에 맞춰 주석 해제하세요)
                        // playerStat.attack.AddValue(totalBonusValue);
                        break;
                        // case Define.UpgradeType.Speed:
                        //     playerStat.speed.AddValue(totalBonusValue);
                        //     break;
                }
            }
        }

        Debug.Log("[UpgradeManager] 영구 업그레이드 스탯 누적 합산이 성공적으로 적용되었습니다!");
    }
}