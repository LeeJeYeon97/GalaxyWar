using TMPro;
using Unity.Services.Economy.Model;
using UnityEngine;
using UnityEngine.UI;

public class UI_ShopItem : UI_Base
{
    enum Texts 
    { 
        Text_Name, 
        Text_Price, 
        Text_Amount, 
    }
    enum Images 
    { 
        Image_Icon, 
        Image_CurrencyIcon 
    }
    enum Buttons 
    {
        Button_Purchase
    }

    private string _purchaseId;

    public override void Init()
    {
        Bind<TMP_Text>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<Button>(typeof(Buttons));

    }

    //  서버 데이터를 UI에 주입하는 함수
    public void SetInfo(VirtualPurchaseDefinition data, Sprite icon, Sprite currencyIcon)
    {
        _purchaseId = data.Id;

        // 1. 텍스트 정보 세팅 (Economy 대시보드에 입력한 Name)
        GetTMP((int)Texts.Text_Name).text = data.Name;

        // 2. 가격 정보 세팅 (Costs 리스트의 첫 번째 항목 기준)
        var cost = data.Costs[0];
        GetTMP((int)Texts.Text_Price).text = cost.Amount.ToString("N0");

        // 3. 보상 정보 세팅 (Rewards 리스트)
        var reward = data.Rewards[0];
        GetTMP((int)Texts.Text_Amount).text = $"x{reward.Amount}";

        // 4. 아이콘 세팅
        GetImage((int)Images.Image_Icon).sprite = icon;
        GetImage((int)Images.Image_CurrencyIcon).gameObject.SetActive(true);
        GetImage((int)Images.Image_CurrencyIcon).sprite = currencyIcon;


        Button btn = GetButton((int)Buttons.Button_Purchase);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => Managers.VirtualStore.PurchaseVirtualItem(_purchaseId));
    }
    public void SetInfo(RealMoneyPurchaseDefinition data, Sprite icon)
    {
        _purchaseId = data.Id;
        GetTMP((int)Texts.Text_Name).text = data.Name;

        // 핵심: 스토어에서 현지화된 가격 문자열 가져오기
        string price = Managers.IAPStore.GetLocalizedPrice(data.Id);
        GetTMP((int)Texts.Text_Price).text = price;

        // 현금 결제는 보통 재화 아이콘이 필요 없으므로 숨기거나 전용 아이콘 배치
        GetImage((int)Images.Image_CurrencyIcon)?.gameObject.SetActive(false);
        GetImage((int)Images.Image_Icon).sprite = icon;


        if (data.Rewards.Count > 0)
        {
            GetTMP((int)Texts.Text_Amount).text = $"x{data.Rewards[0].Amount}";
        }
        Button btn = GetButton((int)Buttons.Button_Purchase);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => Managers.IAPStore.PurchaseRealMoneyProduct(_purchaseId));
    }

    // 광고 전용 정보 주입 함수
    public void SetInfoForAd(string title, string amountText, Sprite icon, Sprite currencyIcon, string placementName)
    {
        GetTMP((int)Texts.Text_Name).text = title;
        GetTMP((int)Texts.Text_Price).text = "FREE"; // 가격 대신 무료 표시
        GetTMP((int)Texts.Text_Amount).text = amountText;

        GetImage((int)Images.Image_Icon).sprite = icon;
        GetImage((int)Images.Image_CurrencyIcon).gameObject.SetActive(true);
        GetImage((int)Images.Image_CurrencyIcon).sprite = currencyIcon;


        Button btn = GetButton((int)Buttons.Button_Purchase);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => Managers.AD.ShowRewardedAd(placementName));
    }

}
