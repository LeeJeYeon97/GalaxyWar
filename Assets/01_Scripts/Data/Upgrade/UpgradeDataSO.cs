using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class UpgradeBalanceData
{
    public string type;          // Enum 번역용 (예: "HP", "Damage")
    public string upgradeName;   // 이름
    public UpgradeLevelData[] levelInfos; // 레벨별 비용/스탯/설명 배열!
}

[System.Serializable]
public class UpgradeConfigWrapper
{
    public List<UpgradeBalanceData> upgradeList;
}


//  [System.Serializable]을 꼭 붙여야 유니티 인스펙터 창에서 리스트로 보입니다!
[System.Serializable]
public class UpgradeLevelData
{
    public int cost;            // 이 레벨을 올리기 위해 필요한 비용
    public float statValue;     // 이 레벨에 도달했을 때 오르는 실제 수치 (예: 150, 200)
    public string description;  // 이 레벨의 설명 (예: "HP가 크게 증가합니다.")
}

// 이 스크립트를 만들면 유니티에서 우클릭으로 진화 데이터를 생성할 수 있습니다.
[CreateAssetMenu(fileName = "NewUpgradeData", menuName = "Data/UpgradeData")]
public class UpgradeDataSO : ScriptableObject
{
    public Define.UpgradeType type;
    public string upgradeName;      // 예: "체력 강화"
    public Sprite iconSprite;       // 대표 아이콘 1개

    //  핵심: 레벨별 데이터를 배열(리스트)로 관리합니다!
    public UpgradeLevelData[] levelInfos;

    // 현재 최대 레벨이 몇인지 배열의 길이로 바로 알 수 있습니다.
    public int MaxLevel => levelInfos.Length;

    // 현재 레벨을 넣으면, '다음 레벨업'에 필요한 정보를 뽑아주는 편리한 함수
    public UpgradeLevelData GetNextLevelInfo(int currentLevel)
    {
        // 만렙이면 더 이상 다음 정보가 없으므로 마지막 정보를 줍니다.
        if (currentLevel >= MaxLevel)
        {
            return levelInfos[MaxLevel - 1];
        }

        // 배열은 0번부터 시작하므로, 현재 레벨이 0이면 levelInfos[0]을 줍니다.
        return levelInfos[currentLevel];
    }
}

