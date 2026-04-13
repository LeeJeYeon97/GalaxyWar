using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_ShopPanel : UI_Base
{
    enum Buttons
    {
        Button_RemoveAd_IAP,       // 광고 제거 (IAP)
        Button_PurchaseCoins_IAP,   // 코인 구매 (IAP)
        Button_PurchaseCoins_AD,    // 코인 획득 (광고 시청)
        Button_PurchaseItem_VP,     // 아이템 구매 (가상 재화)
    }

    public override void Init()
    {
        base.Init(); 

        // 1. 버튼 바인딩
        Bind<Button>(typeof(Buttons));

        // 2. 버튼 이벤트 연결
        GetButton((int)Buttons.Button_RemoveAd_IAP).onClick.AddListener(OnClickRemoveAds);
        GetButton((int)Buttons.Button_PurchaseCoins_IAP).onClick.AddListener(OnClickBuyCoinsIAP);
        GetButton((int)Buttons.Button_PurchaseCoins_AD).onClick.AddListener(OnClickBuyCoinsAD);
        GetButton((int)Buttons.Button_PurchaseItem_VP).onClick.AddListener(OnClickBuyItemVP);
    }

    // --- [클릭 이벤트 함수들] ---

    // 1. 광고 제거 결제 (실제 돈)
    private void OnClickRemoveAds()
    {
        Managers.Sound.Play(SoundID.Sfx_UIButtonClick);

        // 아직 ID가 선언 안 되어 있다면 Define에 추가 후 사용하세요.
        // Managers.IAPStore.PurchaseRealMoneyProduct("REMOVE_ADS_ID");
        Debug.Log("광고 제거 결제 시도");
    }

    // 2. 코인 묶음 결제 (실제 돈)
    private void OnClickBuyCoinsIAP()
    {
        Managers.Sound.Play(SoundID.Sfx_UIButtonClick);
        // 기존 UI_LobbyScene의 OnClickBuyCoinsButton 로직 이동
        Managers.IAPStore.PurchaseRealMoneyProduct(k_goldPurchase100Id);
    }

    // 3. 광고 보고 코인 얻기 (보상형 광고)
    private void OnClickBuyCoinsAD()
    {
        Managers.Sound.Play(SoundID.Sfx_UIButtonClick);
        // 아까 만든 광고 매니저 호출 (PlacementName 전달)
        Managers.AD.ShowRewardedAd(placementShopGoldAd);
    }

    // 4. 가상 재화로 아이템 구매 (게임 내 코인 소모)
    private void OnClickBuyItemVP()
    {
        Managers.Sound.Play(SoundID.Sfx_UIButtonClick);
        // 기존 UI_LobbyScene의 OnClickBuyItem 로직 이동
        Managers.VirtualStore.PurchaseVurtualItem(Define.k_HealthPotionPurchaseId);
    }

}
