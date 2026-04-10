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
    // v5 Controller (one stop for fetching, purchasing, confirming)
    private StoreController _storeController;

    // 서버 스토어 로직 클래스
    private StoreServiceBindings _storeServiceBindings;

    // 구매 진행중 여부 체크 변수
    private bool _isPurchaseInProgress;

    private CrossPlatformValidator _crossPlatformValidator;

    public event Action<string> SuccessfullyPurchased;
    public event Action<string> PurchaseFailed;

    public void Init()
    {
        Managers.PlayerEconomy.EconomyConfigSynced -= InitializeIAPSync;
        Managers.PlayerEconomy.EconomyConfigSynced += InitializeIAPSync;

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
        UnsubscribeIAPEvents();
    }

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
        Debug.Log($"[IAP] OnPurchaseDeferred : {deferred.Info}");
    }
    private async void OnPurchasePending(PendingOrder pending)
    {
        try
        {
            Debug.Log($"Full receipt JSON : {pending.Info.Receipt}");

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
            Debug.Log($"[IAP] Pending purchase : {product.definition.id}");

            var receipt = pending.Info.Receipt;

            // Optional Google validation (Apple handled internally in v5)
            if (!ValidateIfGoogle(receipt))
            {
                Debug.LogError("[IAP] Google receipt validation failed.");
                PurchaseFailed?.Invoke("Invalid receipt for " + product.definition.id);
                return;
            }

            // Cloud Code validation + grant
            PlayerEconomyData updated = await _storeServiceBindings.ProcessRealMoneyPurchase(
                product.definition.id,
                receipt,
                (double)product.metadata.localizedPrice,    //Cloud Code bindings don't support decimals
                product.metadata.isoCurrencyCode);

            Managers.PlayerEconomy.HandleEconomyUpdate(updated);

            // confirm the order to finalize with the store
            _storeController.ConfirmPurchase(pending);
            Debug.Log($"[IAP] Confirmed purchase : {product.definition.id}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IAP] Error processing pending order : {ex.Message}");
            PurchaseFailed?.Invoke("Purchase failed : " + ex.Message);
        }
    }
    private void OnPurchaseConfirmed(Order order)
    {
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
        _isPurchaseInProgress = false;

        Debug.LogError($"[IAP] Purchase failed : {failed.FailureReason.ToString()}");
        PurchaseFailed.Invoke($"Purchase failed : {failed.FailureReason.ToString()}");

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
            // 제품 정의 목록 만들기?
            var productDefs = BuildProductDefinitionsFromEconomy();

            if (productDefs.Count == 0)
            {
                Debug.Log("[IAP] No real-money products found in Economy config.");
                return;
            }


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
        var productDefinitions = new List<ProductDefinition>();

        // 실제 현금 구매 정의 목록 가져오기
        var realMoneny = EconomyService.Instance.Configuration.GetRealMoneyPurchases();

        foreach(var purchase in realMoneny)
        {
            // 가져온 현금 구매 정의를 IAP 제품 정의 객체로 변환
            // 소모성 아이템으로 정의 -> 광고제거 같은 거는 코드나 IAP카탈로그에서 진행 해야함?
            var def = new ProductDefinition(id: purchase.Id, type: ProductType.Consumable);
            productDefinitions.Add(def);
        }

        Debug.Log($"[IAP] Prepared {productDefinitions.Count} ProductDefinitions for fetch.");
        return productDefinitions;
    }
    private void LogRealPurchasesFromConfig(List<RealMoneyPurchaseDefinition> realMoneyPurchases)
    {
        Debug.Log($"Real purchases from economy config :\n{JsonConvert.SerializeObject(realMoneyPurchases, Formatting.Indented)}");
    }

    // Alternative : Using CatalogProvider instead of Economy configuration
    private void BuildAndFetchProductsWithCatalog()
    {
        // Load Catalog from Assets/Resources/IAPProcutCatalog.json
        var catalog = ProductCatalog.LoadDefaultCatalog();
        if(catalog == null || catalog.allProducts == null || catalog.allProducts.Count == 0)
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

    public void PurchaseGold()
    {
        if(_isPurchaseInProgress)
        {
            Debug.LogWarning("[IAP] Purchase already in progress");
            return;
        }
        _isPurchaseInProgress = true;

        // 여기서 구매대기 이벤트 발생
        // In v5, you can purchase by id directly via the controller
        _storeController.PurchaseProduct(Define.k_goldPurchase100Id);
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
}
