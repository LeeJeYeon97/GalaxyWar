using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings.Project;
using Unity.Services.Economy.Model;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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

    // 팁: 테이블 이름이 바뀌면 여기서 한 번만 수정하면 됩니다.
    private const string UI_TABLE_NAME = "UI";
    private ShopItemDataSO _localData;
    private VirtualPurchaseDefinition _vData;
    private RealMoneyPurchaseDefinition _rData;


    public override void Init()
    {
        Bind<TMP_Text>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<Button>(typeof(Buttons));

    }

    // [추가] 오브젝트가 활성화될 때 유니티 로컬라이제이션의 '언어 변경 이벤트'를 구독합니다.
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        Managers.IAPStore.SuccessfullyPurchased -= OnChangedText;
        Managers.IAPStore.SuccessfullyPurchased += OnChangedText;
    }

    //  [추가] 오브젝트가 비활성화되거나 파괴될 때 메모리 누수 방지를 위해 구독을 해제합니다.
    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        Managers.IAPStore.SuccessfullyPurchased -= OnChangedText;
    }
    // [추가] 유저가 기기나 설정창에서 언어를 바꾸면 유니티가 이 함수를 자동으로 실행해 줍니다.
    private void OnLocaleChanged(Locale locale)
    {
        if (_localData == null) return;

        Debug.Log($"[ShopUI] 언어가 {locale.Identifier.Code}로 변경되어 상점 텍스트를 재갱신합니다.");
        // 언어가 바뀔 때 유저가 그 사이에 보상을 받았을 수도 있으니 상태를 먼저 체크합니다.
        RefreshItemState();
        UpdateLocalizationTexts();
    }

    private void OnChangedText(string message)
    {
        UpdateLocalizationTexts();
    }
    
    public void SetInfo(ShopItemDataSO localData, VirtualPurchaseDefinition vData = null, RealMoneyPurchaseDefinition rData = null)
    {
        // 데이터 캐싱 (기억해두기)
        _localData = localData;
        _vData = vData;
        _rData = rData;

        // 1. 시각적 이미지 데이터 세팅 (이미지는 언어와 무관하므로 기존대로 처리)
        GetImage((int)Images.Image_Icon).sprite = localData.mainIcon;
        if (localData.currencyIcon != null)
        {
            GetImage((int)Images.Image_CurrencyIcon).sprite = localData.currencyIcon;
        }

        // 버튼 리스너 초기화 및 연결
        Button btn = GetButton((int)Buttons.Button_Purchase);
        btn.onClick.RemoveAllListeners();

        // 일일보상 받았으면 버튼 비활성화 및 텍스트 설정


        // 결제 타입 분기 및 ID 세팅
        if (rData != null)
        {
            _purchaseId = rData.Id;
            btn.onClick.AddListener(() => Managers.IAPStore.PurchaseRealMoneyProduct(_purchaseId));
        }
        else if (vData != null)
        {
            _purchaseId = vData.Id;
            btn.onClick.AddListener(() => Managers.VirtualStore.PurchaseVirtualItem(_purchaseId));
        }
        else
        {
            if (localData.type == Define.ShopItemType.GOLD_AD)
            {
                btn.onClick.AddListener(() =>
                {
                    Managers.AD.ShowRewardedAd(localData.placementId, (success) => { });
                });
            }
            else if (localData.type == Define.ShopItemType.GOLD_FREE)
            {
                btn.onClick.AddListener(async () =>
                {
                    btn.interactable = false;
                    Managers.UI.ShowPopupUI<UI_LoadingPopup>();
                    bool isSuccess = await Managers.IAPStore.ClaimDailyFreeRewardAsync(localData.rewardAmount);

                    Managers.UI.ClosePopupUI();
                    if (isSuccess)
                    {

                        // 상태와 텍스트를 즉시 새로고침
                        // 수정됨: this.transform 대신 btn.transform을 사용하여 '버튼'에서 코인이 터지도록 변경!
                        _ = UIEffectUtil.PlayCoinFlyEffect(btn.transform.position, 10); // 개수도 10개 정도로 늘리면 더 예쁩니다.
                        Managers.Sound.Play(SoundID.Sfx_GetGold);
                        RefreshItemState();
                        UpdateLocalizationTexts();
                    }
                    else
                    {
                        btn.interactable = true;
                    }
                });
            }
        }

        // 2. [순서 핵심] 현재 아이템의 구매 상태(버튼 interactable)를 먼저 갱신합니다.
        RefreshItemState();

        // 2. 실제 언어의 영향을 받는 텍스트들만 따로 모아서 그려줍니다.
        UpdateLocalizationTexts();
    }

    //  [추가] 오직 '다국어 번역'이 필요한 텍스트 컴포넌트들만 실시간으로 새로고침하는 핵심 메서드
    private void UpdateLocalizationTexts()
    {
        if (_localData == null) return;

        // 1) 아이템 타이틀 다국어 반영
        GetTMP((int)Texts.Text_Name).text = _localData.localizedTitle.GetLocalizedString();

        // 2) 수량 텍스트 처리
        if (string.IsNullOrEmpty(_localData.amountText))
        {
            GetTMP((int)Texts.Text_Amount).gameObject.SetActive(false);
        }
        else
        {
            GetTMP((int)Texts.Text_Amount).gameObject.SetActive(true);
            GetTMP((int)Texts.Text_Amount).text = $"x{_localData.amountText}";
        }

        // 3) 가격 및 버튼 상태 텍스트 다국어 반영
        if (_rData != null)
        {
            // 현금 상품 현지화 가격 (스토어가 알아서 통화 기호를 맞춰줌)
            string price = Managers.IAPStore.GetLocalizedPrice(_rData.Id);
            GetTMP((int)Texts.Text_Price).text = price;
            GetImage((int)Images.Image_CurrencyIcon).gameObject.SetActive(false);

            if (_rData.Rewards.Count > 0)
            {
                GetTMP((int)Texts.Text_Amount).text = $"x{_rData.Rewards[0].Amount}";
            }

            if(_rData.Id == Define.k_IAP_RemoveAd && Managers.PlayerData.PlayerDataLocal.IsAdsRemoved)
            {
                GetButton((int)Buttons.Button_Purchase).interactable = false;
                string textKey = "ShopItem_PurchaseComplete";
                GetTMP((int)Texts.Text_Price).text = LocalizationSettings.StringDatabase.GetLocalizedString(UI_TABLE_NAME, textKey);
            }

        }
        else if (_vData != null)
        {
            // 가상 재화 상품 가격
            var cost = _vData.Costs[0];
            GetTMP((int)Texts.Text_Price).text = cost.Amount.ToString("N0");
            GetImage((int)Images.Image_CurrencyIcon).gameObject.SetActive(true);
            if(_vData.Rewards.Count > 0)
            {
                GetTMP((int)Texts.Text_Amount).text = $"x{_vData.Rewards[0].Amount}";
            }
        }
        else
        {
            // 로컬 전용 상품 (무료 / 광고)
            if (_localData.type == Define.ShopItemType.GOLD_AD)
            {
                GetImage((int)Images.Image_CurrencyIcon).gameObject.SetActive(true);
                GetTMP((int)Texts.Text_Price).gameObject.SetActive(false);
            }
            else if (_localData.type == Define.ShopItemType.GOLD_FREE)
            {
                GetImage((int)Images.Image_CurrencyIcon).gameObject.SetActive(false);

                // 현재 버튼이 비활성화 상태(이미 받음)라면 '수령 완료' 번역을, 아니면 '무료' 번역을 유연하게 출력합니다.
                Button btn = GetButton((int)Buttons.Button_Purchase);
                string textKey = btn.interactable ? "ShopItem_Free" : "ShopItem_Claimed";

                GetTMP((int)Texts.Text_Price).text = LocalizationSettings.StringDatabase.GetLocalizedString(UI_TABLE_NAME, textKey);
            }
        }
    }
    // [추가] 오늘 이미 무료 상품을 수령했는지 체크하여 버튼의 활성 상태를 제어하는 함수
    private void RefreshItemState()
    {
        if (_localData == null) return;

        Button btn = GetButton((int)Buttons.Button_Purchase);

        if (_localData.type == Define.ShopItemType.GOLD_FREE)
        {
            // 1. 한국 시간(KST) 오늘 날짜 구하기
            DateTime kstNow = DateTime.UtcNow.AddHours(9);
            string todayStr = kstNow.ToString("yyyy-MM-dd");

            // 2. 로그인할 때 받아와서 클라 메모리에 들고 있는 최신 PlayerData를 참조합니다.
            // (주의: 대표님의 클라이언트 Managers 구조 중 PlayerData 객체 접근 경로로 수정해 주세요!)
            string lastClaimDate = Managers.PlayerData.PlayerDataLocal.LastDailyFreeGoldClaimDate;

            // 3. 오늘 이미 받았다면 버튼을 잠그고(false), 안 받았다면 열어줍니다(true).
            bool isAlreadyClaimed = (lastClaimDate == todayStr);
            btn.interactable = !isAlreadyClaimed;
        }
        else
        {
            // 무료 상품이 아니거나 일회성 패키지가 아니라면 기본적으로 버튼 활성화
            btn.interactable = true;
        }
    }
}
