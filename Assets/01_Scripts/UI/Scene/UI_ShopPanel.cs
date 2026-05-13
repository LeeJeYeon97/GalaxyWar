using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_ShopPanel : UI_Base
{

    //[SerializeField] private Transform _content; // 스크롤 뷰의 Content

    [Header("카테고리별 아이템 바구니(Container)")]
    [SerializeField] private Transform _removeAdContainer; // 추가: Container_RemoveAd 연결
    [SerializeField] private Transform _goldContainer;     // 추가: Container_Gold 연결
    [SerializeField] private Transform _packageContainer;  // 추가: Container_Package 연결 (필요시)

    [Header("프리팹")]
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private GameObject _mainItemPrefab;

    // 현재는 광고 제거 현금 상품만 등록할것
    // 리소스 이름 규칙화: 서버에 등록한 Purchase ID와 유니티 Resources 폴더(혹은 Addressables) 안의 이미지 이름을 똑같이 맞춥니다. (예: ID가 POTION_HP면 이미지 이름도 POTION_HP)

    public override void Init()
    {
        base.Init();

        
        //이제 _content 전체가 아니라, 각 바구니(Container) 안의 아이템만 청소합니다!
        ClearContainer(_removeAdContainer);
        ClearContainer(_goldContainer);
        // ClearContainer(_packageContainer);


        if (_itemPrefab == null)
        {
            _itemPrefab = Managers.Resource.Load<GameObject>("UI/SubItem/UI_StoreItem");
        }
        if (_mainItemPrefab == null)
        {
            _itemPrefab = Managers.Resource.Load<GameObject>("UI/SubItem/UI_StoreMainItem");
        }
        // 광고 보상 상품 아직은 없음
        //CreateAdRewardItem();

        var virtualPurchases = EconomyService.Instance.Configuration.GetVirtualPurchases();
        // 3. 가상 상품 생성 루프
        foreach (var purchase in virtualPurchases)
        {
            CreateVirtualItem(purchase, _goldContainer);
        }

        // 현금 상품(광고제거 등) 생성
        var realMoneyPurchases = EconomyService.Instance.Configuration.GetRealMoneyPurchases();
        foreach (var purchase in realMoneyPurchases)
        {
            // 광고 제거/현금 상품이므로 알맞은 바구니를 넘겨줍니다.
            Transform targetContainer = (purchase.Id == Define.k_IAP_RemoveAd) ? _removeAdContainer : _packageContainer;
            CreateRealMoneyItem(purchase, targetContainer);
        }

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
    private void CreateAdRewardItem()
    {
        //GameObject go = Managers.Resource.Instantiate(_itemPrefab, _content);
        //UI_ShopItem item = go.GetOrAddComponent<UI_ShopItem>();
        //
        //// 하드코딩 데이터 설정
        //string title = "무료 골드";
        //string amountText = "x50";
        //Sprite icon = Managers.Resource.Load<Sprite>("Sprites/UI_Icon_GiftBox"); // 선물상자 아이콘 등
        //Sprite currencyIcon = Managers.Resource.Load<Sprite>("Sprites/GOLD");  // 골드 아이콘
        //string placement = "Shop_Free_Gold"; // 아이언소스 플레이스먼트 이름
        //
        //item.SetInfoForAd(title, amountText, icon, currencyIcon, placement);
    }

    private void CreateVirtualItem(VirtualPurchaseDefinition data, Transform parentContainer)

    {
        // 3. 프리팹 생성
        GameObject go = Managers.Resource.Instantiate(_itemPrefab, parentContainer);
        UI_ShopItem item = go.GetOrAddComponent<UI_ShopItem>();

        // 4. 아이콘 데이터 매칭 (아래 3번 항목 참고)
        Sprite mainIcon = Managers.Resource.Load<Sprite>($"Sprites/{data.Id}"); // ID와 동일한 이름의 이미지 로드

        string currencyId = data.Costs[0].Item.GetReferencedConfigurationItem().Id;
        Sprite currencyIcon = Managers.Resource.Load<Sprite>($"Sprites/{currencyId}");

        // 5. 정보 주입
        item.SetInfo(data, mainIcon, currencyIcon);
    }
    private void CreateRealMoneyItem(RealMoneyPurchaseDefinition data , Transform parentContainer)
    {
        // 광고 제거 상품이면
        GameObject go;
        
        if (data.Id == Define.k_IAP_RemoveAd)
        {
            go = Managers.Resource.Instantiate(_mainItemPrefab, parentContainer);
        }
        else
        {
            go = Managers.Resource.Instantiate(_itemPrefab, parentContainer);
        }
        UI_ShopItem item = go.GetOrAddComponent<UI_ShopItem>();
        // 1번 방식(이름 규칙화) 적용
        Sprite icon = Managers.Resource.Load<Sprite>($"Sprites/{data.Id}");

        Debug.Log("광고 제거 상품 아이템 세팅 시작");
        item.SetInfo(data, icon);
    }

}
