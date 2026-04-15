using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using Unity.Services.CloudCode.GeneratedBindings.Project;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

public class IAPStoreManager
{
    // TODO 
//    실제 서비스에서는 유저의 인터넷이 불안정해서 Connect나 FetchProducts가 실패할 수도 있습니다.

//이런 경우를 대비해 **'재시도 버튼'**을 UI에 만들어두고, 클릭 시 다시 InitializeIAPSync() 를 호출하게 하면 훨씬 안정적인 상점을 만들 수 있습니다.

//지금 유저님의 코드는 try-catch로 에러를 잘 잡고 있으니, 에러 로그 출력 시점에 "네트워크를 확인해 주세요"라는 팝업을 띄우는 로직만 추가하면 상용 수준의 코드가 됩니다!



    // 최신 Unity IAP v5 컨트롤러 (상품 조회, 구매, 확정을 모두 담당하는 매니저)
    // v5 Controller (one stop for fetching, purchasing, confirming)
    private StoreController _storeController;

    // 서버의 현금 결제(ProcessRealMoneyPurchase) 함수를 부르기 위한 리모컨
    // 서버 스토어 로직 클래스
    private StoreServiceBindings _storeServiceBindings;

    // 구매 진행중 여부 체크 변수
    private bool _isPurchaseInProgress;

    // 안드로이드(구글) 로컬 영수증 1차 검증기
    private CrossPlatformValidator _crossPlatformValidator;

    public event Action<string> SuccessfullyPurchased;
    public event Action<string> PurchaseFailed;

    #region Core
    public void Init()
    {
        // 1. UGS의 상점 데이터 다운로드가 끝나면 -> IAP 초기화 시작
        Managers.PlayerEconomy.EconomyConfigSynced -= InitializeIAPSync;
        Managers.PlayerEconomy.EconomyConfigSynced += InitializeIAPSync;

        // 2. 유니티 서비스 초기화 시 서버 리모컨 조립
        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
        Managers.Initialize.OnUnityServiceInit += SetupBindings;
    }
    public void SetupBindings()
    {
        _storeServiceBindings = new StoreServiceBindings(CloudCodeService.Instance);
    }
    private void Clear()
    {

        Managers.PlayerEconomy.EconomyConfigSynced -= InitializeIAPSync;
        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
        UnsubscribeIAPEvents();
    }

    // IAP v5의 모든 이벤트를 구독합니다.
    private void SubscribeIAPEvents()
    {
        if (_storeController == null) return;

        _storeController.OnProductsFetched += OnProductsFetched;
        _storeController.OnProductsFetchFailed += OnProductsFetchFailed;

        _storeController.OnPurchasesFetched += OnPurchasesFetched;
        _storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

        _storeController.OnPurchasePending += OnPurchasePending;
        _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        _storeController.OnPurchaseFailed += OnPurchaseFailed;

        _storeController.OnPurchaseDeferred += OnPurchaseDeferred;

        _storeController.OnStoreDisconnected += OnStoreDisconnected;
    }
    private void UnsubscribeIAPEvents()
    {
        if (_storeController == null) return;

        _storeController.OnProductsFetched -= OnProductsFetched;
        _storeController.OnProductsFetchFailed -= OnProductsFetchFailed;

        _storeController.OnPurchasesFetched -= OnPurchasesFetched;
        _storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;

        _storeController.OnPurchasePending -= OnPurchasePending;
        _storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
        _storeController.OnPurchaseFailed -= OnPurchaseFailed;

        _storeController.OnPurchaseDeferred -= OnPurchaseDeferred;

        _storeController.OnStoreDisconnected -= OnStoreDisconnected;
    }


    #region Product/Purchase fetch callbacks
    private void OnProductsFetched(List<Product> products)
    {
        // Init 한 후에 불림


        // 스토어에서 상품 정보를 성공적으로 가져왔습니다!
        // 여기서 FetchPurchases()를 부르면 유저가 과거에 샀던 '비소모성(광고제거 등)' 아이템 내역을 복구해 옵니다.
        // 구매한 상품에 대해 과거 구매내역 조회하고 사용가능한 상품을 기록
        _storeController.FetchPurchases();

        LogProductsFetched(products);
    }
    private void LogProductsFetched(List<Product> products)
    {
        Debug.Log($"[IAP] Products fetched :{products.Count}");
        foreach(var p in products)
        {
            Debug.Log($"[IAP] {p.definition.id} | {p.metadata.localizedTitle} | {p.metadata.localizedPriceString}");
        }
    }
    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogError($"[IAP] Product fetch failed : {failure.FailureReason}");
    }

    void OnPurchasesFetched(Orders orders)
    {
        // Process purchases, e.g. check for entitlements from completed orders
    }
    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        Debug.LogError($"[IAP] Purchases fetch failed : {failure.FailureReason}");
    }
    private void OnStoreDisconnected(StoreConnectionFailureDescription desc)
    {
        Debug.LogError($"[IAP] Store disconnected : {desc.Message}");
    }
    #endregion

    private void OnPurchaseDeferred(DeferredOrder deferred)
    {
        // 5. '부모 승인 요청' 상태가 되면 일단 유저가 할 일은 끝났으므로 로딩 OFF!
        Managers.UI.ClosePopupUI();
        _isPurchaseInProgress = false;
        Debug.Log($"[IAP] 구매 승인 대기 중 (Ask to Buy)");
    }

    //[핵심] 결제 대기 상태(구글/애플에서 결제는 성공했고, 우리가 보상을 줄 차례)
    private async void OnPurchasePending(PendingOrder pending)
    {
        try
        {
            Debug.Log($"Full receipt JSON : {pending.Info.Receipt}");

            // 1. 장바구니에서 유저가 산 상품의 ID를 꺼냅니다.
            // v5 : products live in the order's cart (usually 1 item, but don't assume)
            var firstItem = pending.CartOrdered.Items().FirstOrDefault();
            var pid = firstItem?.Product?.definition?.id;

            if (string.IsNullOrEmpty(pid))
            {
                Debug.LogError("[IAP] Pending order has no product id.");
                PurchaseFailed?.Invoke("No product id in pending order");
                return;
            }
            var product = _storeController?.GetProductById(pid);
            if (product == null)
            {
                Debug.LogError($"[IAP] Product not found in controller : {pid}");
                PurchaseFailed?.Invoke($"Product not found : {pid}");
                return;
            }
            // 1. 영수증(Receipt) 추출
            var receipt = pending.Info.Receipt;

            // 2. 안드로이드라면 서버에 보내기 전에 클라이언트에서 1차로 가짜 영수증인지 검사합니다.
            // Optional Google validation (Apple handled internally in v5)
            if (!ValidateIfGoogle(receipt))
            {
                Debug.LogError("[IAP] Google receipt validation failed.");
                PurchaseFailed?.Invoke("Invalid receipt for " + product.definition.id);
                return;
            }
            Debug.Log($"[IAP] Pending purchase : {product.definition.id}");
            Debug.Log($"[IAP] 서버 검증 요청 시작 : {product.definition.id}");

            // 3. 서버(Cloud Code)에 영수증을 보내서 2차 깐깐한 검증을 받고 보상을 지급받습니다!
            // Cloud Code validation + grant
            PlayerEconomyData updated = await _storeServiceBindings.ProcessRealMoneyPurchase(
                product.definition.id,
                receipt,
                (double)product.metadata.localizedPrice,    //Cloud Code bindings don't support decimals
                product.metadata.isoCurrencyCode);

            // 4. 보상이 잘 들어왔으니 내 지갑(로컬 데이터)을 최신화합니다.
            Managers.PlayerEconomy.HandleEconomyUpdate(updated);

            // 5. 서버 보상까지 완벽히 끝났으니, 스토어에 "결제 확정(Confirm)해 줘!" 라고 알립니다.
            // (이걸 안 부르면 며칠 뒤에 유저에게 환불 처리됩니다.)
            _storeController.ConfirmPurchase(pending);
            Debug.Log($"[IAP] Confirmed purchase : {product.definition.id}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IAP] Error processing pending order : {ex.Message}");
            PurchaseFailed?.Invoke("Purchase failed : " + ex.Message);

            // 서버 검증에서 에러가 나면 여기서 로딩을 닫아줘야 유저가 다시 시도할 수 있습니다.
            Managers.UI.ClosePopupUI();
            _isPurchaseInProgress = false;
        }
    }
    private void OnPurchaseConfirmed(Order order)
    {
        // 3. 모든 과정(서버 보상 + 스토어 확정)이 끝났을 때 로딩 OFF!
        Managers.UI.ClosePopupUI();
        _isPurchaseInProgress = false;

        if (order is FailedOrder failedOrder)
        {
            Debug.LogWarning($"[IAP] Confirmation failed: {failedOrder.FailureReason}");
            return;
        }

        var purchasedProduct = order.CartOrdered.Items().FirstOrDefault()?.Product;

        Debug.Log($"[IAP] Purchase confirmed : {purchasedProduct?.definition.id} | Tx : {order.Info?.TransactionID}");
        SuccessfullyPurchased?.Invoke($"Purchase confirmed : {purchasedProduct?.definition.id}");
    }

    private void OnPurchaseFailed(FailedOrder failed)
    {
        // 결제 도중 취소하거나 잔액이 부족해 실패한 경우 락(Lock)을 풀어줍니다.
        _isPurchaseInProgress = false;
        Managers.UI.ClosePopupUI();

        Debug.LogError($"[IAP] Purchase failed : {failed.FailureReason.ToString()}");
        PurchaseFailed?.Invoke($"Purchase failed : {failed.FailureReason.ToString()}");

    }
    private async void InitializeIAPSync()
    {
        // Get Controller
        _storeController = UnityIAPServices.StoreController();

        // Subscribe to events
        SubscribeIAPEvents();

        try
        {
            // When this call completes, you may assume that IAP has connected to your current app store.
            await _storeController.Connect();
            Debug.Log("[IAP] Connected to store.");

            // Build and fetch products from economy config
            // UGS 대시보드에 등록해둔 현금 상품들을 유니티 IAP 시스템에 맞게 포장합니다.
            var productDefs = BuildProductDefinitionsFromEconomy();

            if (productDefs.Count == 0)
            {
                Debug.Log("[IAP] No real-money products found in Economy config.");
                return;
            }

            // 스토어(구글/애플)에 상품 목록을 던져주며 "이것들 팔 준비해!" 라고 요청합니다.
            _storeController.FetchProducts(productDefs);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IAP] Connect failed : {ex.Message}");
        }

        // 필요한 경우 영수증 유효성 검사기 초기화
        InitializeReceiptValidatorsIfNeeded();
    }
    private List<ProductDefinition> BuildProductDefinitionsFromEconomy()
    {
        // 1. 주문서(빈 리스트)를 하나 준비합니다.
        var productDefinitions = new List<ProductDefinition>();

        // 2.UGS Economy 서버에서 다운로드해 두었던 '현금 상품 목록'을 전부 가져옵니다.
        // (이 데이터는 게임 켤 때 Managers.PlayerEconomy.Init() 등에서 이미 다운로드된 상태입니다)
        // 실제 현금 구매 정의 목록 가져오기
        var realMoneny = EconomyService.Instance.Configuration.GetRealMoneyPurchases();

        // 3. UGS에서 가져온 상품들을 하나씩 꺼내봅니다.
        foreach (var purchase in realMoneny)
        {
            // 가져온 현금 구매 정의를 IAP 제품 정의 객체로 변환
            // 소모성 아이템으로 정의 -> 광고제거 같은 거는 코드나 IAP카탈로그에서 진행 해야함?
            // 주의: 현재 모든 상품을 '소모성(Consumable)'으로 박아두었습니다. (아래 개선점 참고)
            // 4. UGS 데이터를 Unity IAP가 알아먹을 수 있는 규격(ProductDefinition)으로 포장합니다.
            // "상품 ID는 이거고, 소모성(Consumable) 아이템이야!" 라고 이름표를 붙이는 과정입니다.
            var def = new ProductDefinition(id: purchase.Id, type: ProductType.Consumable);
            productDefinitions.Add(def);
        }

        Debug.Log($"[IAP] Prepared {productDefinitions.Count} ProductDefinitions for fetch.");
        return productDefinitions;
    }
    private void BuildAndFetchProductsWithCatalog()
    {
        // Load Catalog from Assets/Resources/IAPProcutCatalog.json
        var catalog = ProductCatalog.LoadDefaultCatalog();
        if (catalog == null || catalog.allProducts == null || catalog.allProducts.Count == 0)
        {
            Debug.LogWarning("[IAP] No products in IAPProductCatalog.json");
            return;
        }

        var productDefinitions = new List<ProductDefinition>();
        foreach (var item in catalog.allProducts)
        {
            // Convert each catalog item into a  ProductDefinition
            productDefinitions.Add(new ProductDefinition(item.id, item.type));
        }

        // Fetch products from store using Unity IAP Services
        _storeController.FetchProducts(productDefinitions);
    }

    private void LogRealPurchasesFromConfig(List<RealMoneyPurchaseDefinition> realMoneyPurchases)
    {
        Debug.Log($"Real purchases from economy config :\n{JsonConvert.SerializeObject(realMoneyPurchases, Formatting.Indented)}");
    }

    // Alternative : Using CatalogProvider instead of Economy configuration
    
    private void InitializeReceiptValidatorsIfNeeded()
    {
#if !UNITY_EDITOR
        // In v5, Apple receipts are handled by StoreKit2. Keep validator for Googld only.
        if(Application.platform == RuntimePlatform.Android)
        {
            try
            {
                _crossPlatformValidator = new CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier);
                Debug.Log("[IAP] Google receipt validator initialized.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[IAP] Validator init skipped/failed : {e.Message}");
            }
        }
#endif
    }

    private bool ValidateIfGoogle(string receipt)
    {
        if (_crossPlatformValidator == null) return true;

        try
        {
            var result = _crossPlatformValidator.Validate(receipt);
            
            foreach(var r in result)
            {
                Debug.Log($"[IAP] Receipt OK : {r.productID} @ {r.purchaseDate} | Tx : {r.transactionID}");
            }
            return true;
        }
        catch(IAPSecurityException e)
        {
            Debug.LogError($"[IAP] Receipt invalid : {e.Message}");
            return false;
        }
    }
    #endregion

    public void ForceResetPurchaseState()
    {
        _isPurchaseInProgress = false;
        Debug.LogWarning("[IAP] 결제 진행 상태가 강제로 초기화 되었습니다.");
    }

    #region Content
    public void PurchaseRealMoneyProduct(string productId)
    {
        if (_isPurchaseInProgress)
        {
            Debug.LogWarning("[IAP] 이미 결제가 진행 중입니다.");
            return;
        }

        // 상품이 진짜 존재하는지 먼저 검사
        var product = _storeController?.GetProductById(productId);
        if (product == null)
        {
            Debug.LogWarning($"[IAP] 상품을 찾을 수 없습니다: {productId}");
            return;
        }

        //  1. 결제 시작 시 로딩 팝업 ON!
        // (구글/애플 결제창이 뜨기 전까지의 짧은 공백을 메워줍니다)
        Managers.UI.ShowPopupUI<UI_LoadingPopup>();

        _isPurchaseInProgress = true;
        _storeController.PurchaseProduct(productId);
    }
    #endregion

    public string GetLocalizedPrice(string productId)
    {
        var product = _storeController?.GetProductById(productId);
        if (product != null)
        {
            // 스토어가 주는 "1,500" 같은 문자열을 그대로 반환
            return product.metadata.localizedPriceString;
        }
        return "N/A"; // 아직 로드가 안 됐을 경우
    }
}
