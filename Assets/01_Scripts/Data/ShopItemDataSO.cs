using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "ShopItemData", menuName = "Shop/ShopItemData")]
public class ShopItemDataSO : ScriptableObject
{
    [Header("상품 정보")]
    public Define.ShopItemType type;     // 상품 타입
    public Define.ShopCategory category; // 어떤 카테고리인지
    //public string title;                 // 예: "무료 골드"
    // 기존: public string title;
    public LocalizedString localizedTitle; //  변경: 다국어 지원 타이틀
    public string amountText;            // 예: "x50"
    public Sprite mainIcon;              // 예: 선물상자 아이콘
    public Sprite currencyIcon;          // 예: 골드 아이콘
    public GameObject itemPrefab;

    [Header("정렬 순서")]
    [Tooltip("숫자가 낮을수록 상점에서 먼저 보입니다. (예: 무료 상품 = 0, 광고 상품 = 1, 유료 상품 = 2...)")]
    public int sortOrder; // 추가!

    [Header("광고 연동 정보")]
    public string placementId;           // 예: "Shop_Free_Gold" (아이언소스/유니티애즈 등)

    [Header("보상 실제 수치 (로직용)")]
    public string rewardCurrencyId;      // 예: "GOLD" (이코노미에 지급할 재화 ID)
    public int rewardAmount;             // 예: 50

    [Header("서버 매칭 정보")]
    [Tooltip("유니티 이코노미에 등록된 Purchase ID를 적어주세요.")]
    public string economyId; // 예: "GOLD_1000_PACK", "REMOVE_AD" 등
}
