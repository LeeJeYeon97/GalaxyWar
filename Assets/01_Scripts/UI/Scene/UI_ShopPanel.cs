using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_ShopPanel : UI_Base
{

    [Header("카테고리별 아이템 바구니(Container)")]
    [SerializeField] private Transform _removeAdContainer; // 추가: Container_RemoveAd 연결
    [SerializeField] private Transform _goldContainer;     // 추가: Container_Gold 연결
    [SerializeField] private Transform _adContainer;  // 추가: Container_Package 연결 (필요시)
    [SerializeField] private Transform _packageContainer;  // 추가: Container_Package 연결 (필요시)

    // 현재는 광고 제거 현금 상품만 등록할것
    // 리소스 이름 규칙화: 서버에 등록한 Purchase ID와 유니티 Resources 폴더(혹은 Addressables) 안의 이미지 이름을 똑같이 맞춥니다. (예: ID가 POTION_HP면 이미지 이름도 POTION_HP)

    public override void Init()
    {
        base.Init();

        
        //이제 _content 전체가 아니라, 각 바구니(Container) 안의 아이템만 청소합니다!
        ClearContainer(_removeAdContainer);
        ClearContainer(_goldContainer);
        ClearContainer(_adContainer);

        CreateShopItem();
    }

    private void CreateShopItem()
    {
        // 1. 서버 데이터 불러오기 & 딕셔너리 캐싱
        var virtualPurchases = EconomyService.Instance.Configuration.GetVirtualPurchases();
        var realMoneyPurchases = EconomyService.Instance.Configuration.GetRealMoneyPurchases();

        Dictionary<string, VirtualPurchaseDefinition> virtualDict = new Dictionary<string, VirtualPurchaseDefinition>();
        foreach (var v in virtualPurchases) virtualDict[v.Id] = v;

        Dictionary<string, RealMoneyPurchaseDefinition> realDict = new Dictionary<string, RealMoneyPurchaseDefinition>();
        foreach (var r in realMoneyPurchases) realDict[r.Id] = r;

        var sortedShopItems = Managers.Data.ShopItemDataDict.Values
            .OrderBy(data => data.sortOrder)
            .ToList();


        // 2. 로컬 상점 아이템(SO) 순회
        foreach (var data in Managers.Data.ShopItemDataDict.Values)
        {
            // [핵심 1] UI 위치 결정: 오직 'Category'만 봅니다! (결제 방식은 묻지도 따지지도 않음)
            // [위치 결정] category만 보고 어느 바구니에 담을지 결정
            Transform targetContainer = null;
            switch (data.category)
            {
                case ShopCategory.REMOVE_AD: targetContainer = _removeAdContainer; break;
                case ShopCategory.GOLD: targetContainer = _goldContainer; break;
                case ShopCategory.AD: targetContainer = _adContainer; break;
                case ShopCategory.PACKAGE: targetContainer = _packageContainer; break;
            }

            if (targetContainer == null) continue;

            VirtualPurchaseDefinition vData = null;
            RealMoneyPurchaseDefinition rData = null;

            // economyId가 비어있지 않다면 서버 데이터가 있다는 뜻!
            if (!string.IsNullOrEmpty(data.economyId))
            {
                // 가상 상품(보석으로 구매 등) 목록에 있는지 확인
                if (virtualDict.ContainsKey(data.economyId))
                {
                    vData = virtualDict[data.economyId];
                }
                // 현금 상품($결제 등) 목록에 있는지 확인
                else if (realDict.ContainsKey(data.economyId))
                {
                    rData = realDict[data.economyId];
                }
            }
            //  만약 vData와 rData가 둘 다 null이라면? -> 이코노미 서버가 필요 없는 '무료' 또는 '광고' 상품이라는 뜻입니다!
            // 3. 최종 생성 및 데이터 전달
            CreateSOItem(data, targetContainer, vData, rData);
        }
    }
    // 개별 함수(CreateAdRewardItem 등)로 나누지 않고, SO 전용 생성 함수 하나로 통합합니다.
    // 매개변수로 서버 데이터(vData, rData)를 추가로 받습니다. (없는 경우는 null로 들어옵니다)
    private void CreateSOItem(ShopItemDataSO localData, Transform parentContainer, VirtualPurchaseDefinition vData, RealMoneyPurchaseDefinition rData)
    {
        if (localData.itemPrefab == null) return;

        GameObject go = Managers.Resource.Instantiate(localData.itemPrefab, parentContainer);
        UI_ShopItem item = go.GetOrAddComponent<UI_ShopItem>();

        // 3. UI_ShopItem.cs 쪽으로 로컬 정보와 서버 정보를 모두 전달
        item.SetInfo(localData, vData, rData);
    }

    // 중복되는 청소 로직을 함수로 뺐습니다.
    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        foreach (Transform child in container)
        {
            Managers.Resource.Destroy(child.gameObject);
        }
    }
}
