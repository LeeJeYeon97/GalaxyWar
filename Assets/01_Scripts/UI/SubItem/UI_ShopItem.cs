using System.Collections.Generic;
using TMPro;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings.Project;
using Unity.Services.Economy.Model;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;
using static Define;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

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
    public void SetInfo(ShopItemDataSO localData, VirtualPurchaseDefinition vData = null, RealMoneyPurchaseDefinition rData = null)
    {
        // 1. 공통 시각적 데이터 세팅 (클라이언트 SO 기준)
        GetTMP((int)Texts.Text_Name).text = localData.title;
        GetImage((int)Images.Image_Icon).sprite = localData.mainIcon;
        if(localData.currencyIcon != null)
        {
            GetImage((int)Images.Image_CurrencyIcon).sprite = localData.currencyIcon;
        }
        
        if (string.IsNullOrEmpty(localData.amountText))
        {
            GetTMP((int)Texts.Text_Amount).gameObject.SetActive(false);
        }
        else
        {
            GetTMP((int)Texts.Text_Amount).text = localData.amountText;
        }
        // 버튼 리스너 초기화(재사용 풀링 시 중복 클릭 방지)
        Button btn = GetButton((int)Buttons.Button_Purchase);
        btn.onClick.RemoveAllListeners();

        // 2. 결제 타입에 따른 동적 세팅 (우선순위 분기)
        if (rData != null)
        {
            // [현금 결제 상품]
            _purchaseId = rData.Id;

            // 스토어에서 현지화된 가격 문자열 가져오기 (예: 1,500)
            string price = Managers.IAPStore.GetLocalizedPrice(rData.Id);
            GetTMP((int)Texts.Text_Price).text = price;

            // 현금 결제는 재화 아이콘 숨김 처리
            GetImage((int)Images.Image_CurrencyIcon).gameObject.SetActive(false);

            if (rData.Rewards.Count > 0)
            {
                GetTMP((int)Texts.Text_Amount).text = $"x{rData.Rewards[0].Amount}";
            }
            // 현금 결제 이벤트 연결
            btn.onClick.AddListener(() => Managers.IAPStore.PurchaseRealMoneyProduct(_purchaseId));
        }
        else if (vData != null)
        {
            // [가상 재화 결제 상품]
            _purchaseId = vData.Id;

            // 가격 정보 세팅 (Costs 리스트의 첫 번째 항목 기준)
            var cost = vData.Costs[0];
            GetTMP((int)Texts.Text_Price).text = cost.Amount.ToString("N0");

            GetTMP((int)Texts.Text_Amount).text = $"x{vData.Rewards[0].Amount}";
            // 재화 아이콘 활성화 및 세팅
            GetImage((int)Images.Image_CurrencyIcon).gameObject.SetActive(true);
            GetImage((int)Images.Image_CurrencyIcon).sprite = localData.currencyIcon;

            // 가상 결제 이벤트 연결
            btn.onClick.AddListener(() => Managers.VirtualStore.PurchaseVirtualItem(_purchaseId));
        }
        else
        {
            // [서버 데이터가 없는 로컬 상품] (무료 or 광고)
            // 이때 대표님이 만들어두신 localData.type (Define.ShopItemType)을 활용합니다!
            if (localData.type == Define.ShopItemType.GOLD_AD)
            {
                GetImage((int)Images.Image_CurrencyIcon).gameObject.SetActive(true);
                GetTMP((int)Texts.Text_Price).gameObject.SetActive(false);

                // 광고버튼 연결 (바깥쪽 async는 불필요하므로 제거)
                btn.onClick.AddListener(() =>
                {
                    // SO에 세팅해둔 placementId를 그대로 사용합니다!
                    Managers.AD.ShowRewardedAd(localData.placementId, (success) =>
                    {
                        if (success)
                        {
                        }
                        else
                        {
                            // 부활 -> 상점 골드로 로그 수정
                            Debug.Log("상점 골드 광고 시청 실패 또는 취소.");
                        }
                    });
                });
            }
            else if (localData.type == Define.ShopItemType.GOLD_FREE)
            {
                GetImage((int)Images.Image_CurrencyIcon).gameObject.SetActive(false);
                GetTMP((int)Texts.Text_Price).text = "무료";

                btn.onClick.AddListener(async () =>
                {
                    // 1. 중복 클릭을 막기 위해 버튼을 즉시 잠급니다. (따닥 방지)
                    btn.interactable = false;

                    Managers.UI.ShowPopupUI<UI_LoadingPopup>();
                    // 2. 매니저에게 "보상 줘!" 라고 요청하고 결과를 기다립니다.
                    bool isSuccess = await Managers.IAPStore.ClaimDailyFreeRewardAsync(localData.rewardAmount);

                    
                    // 3. 결과에 따른 UI 연출 처리
                    if (isSuccess)
                    {
                        // 성공적으로 받았을 때
                        GetTMP((int)Texts.Text_Price).text = "수령 완료";
                        Managers.UI.ClosePopupUI();
                    }
                    else
                    {
                        // 이미 받았거나 통신에 실패했을 때
                        //GetTMP((int)Texts.Text_Price).text = "수령 완료";
                        // 만약 단순 인터넷 오류로 실패한 거라면 유저가 다시 누를 수 있게 풀어줘야 합니다.
                        btn.interactable = true; 
                        Managers.UI.ClosePopupUI();
                    }
                });
            }
        }
    }
}
