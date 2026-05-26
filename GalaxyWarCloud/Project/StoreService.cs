using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.Economy.Model;

namespace Project;

public class StoreService
{

    private PlayerDataService _playerDataService;
    private PlayerEconomyService _playerEconomyService;
    private readonly ILogger<StoreService> _logger;

    public StoreService(ILogger<StoreService> logger, PlayerEconomyService playerEconomyService, PlayerDataService playerDataService)
    {
        _logger = logger;
        _playerEconomyService = playerEconomyService;
        _playerDataService = playerDataService;
    }

    #region Virtual Purchase (가상 결제)

    [CloudCodeFunction("PurchaseVirtualItem")]
    public async Task<PlayerEconomyData> PurchaseVirtualItem(IExecutionContext context, IGameApiClient gameApiClient, string purchaseId)
    {
        try
        {
            var purchaseResponse = await ProcessVirtualPurchase(context, gameApiClient, purchaseId);

            // 원래는 인벤토리 아이템은 리워드 없이 제공했는데 리워드를 추가하고 코드에서 처리하는 것으로 변경함
            // 3. 궁극의 가로채기(Intercept) 로직 시작!
            // 영수증(Response)에 인벤토리 아이템 보상이 포함되어 있다면?
            // 가로채기(Intercept) 로직 시작!
            if (purchaseResponse.Rewards?.Inventory != null && purchaseResponse.Rewards.Inventory.Count > 0)
            {
                // [핵심 변경 2] GrantInventoryItem이 장비/소모품을 판별할 수 있도록 기획서를 로드합니다!
                var configResponse = await gameApiClient.EconomyConfiguration.GetPlayerConfigurationAsync(
                    context, context.AccessToken, context.ProjectId, context.PlayerId!
                );

                foreach (var grantedItem in purchaseResponse.Rewards.Inventory)
                {
                    string itemKey = grantedItem.Id;
                    int amount = grantedItem.Amount;
                    var newInstanceIds = grantedItem.PlayersInventoryItemIds;

                    // 1. UGS가 만든 껍데기 슬롯들을 모조리 삭제
                    if (newInstanceIds != null)
                    {
                        foreach (var instanceId in newInstanceIds)
                        {
                            await _playerEconomyService.DeleteInventoryItem(context, gameApiClient, instanceId);
                        }
                    }

                    // 2. 우리가 만든 지능형 매니저(GrantInventoryItem)로 토스합니다! (알아서 스택/장비 생성)
                    await _playerEconomyService.GrantInventoryItem(context, gameApiClient, itemKey, amount, configResponse.Data.Results);

                    // 3. 찌꺼기 청소
                    await _playerEconomyService.CleanUpNullOrZeroAmountItems(context, gameApiClient, itemKey);

                    _logger.LogInformation($"[가상결제 아이템 지급 완료] {itemKey} {amount}개");
                }
            }

            // 2. 구매가 끝났으니 최신 정보를 넘겨줍니다. (수동 아이템 지급 코드는 삭제함!)
            return await _playerEconomyService.GetPlayerEconomyData(context, gameApiClient);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, $"Failed to purchase item {purchaseId} for player : {context.PlayerId}");
            throw new Exception($"Failed to purchase item : {ex.Message}", ex);
        }
    }

    // 수정됨: 반환 타입을 Task에서 Task<MakeVirtualPurchaseResponse>로 변경
    private async Task<PlayerPurchaseVirtualResponse> ProcessVirtualPurchase(IExecutionContext context, IGameApiClient gameApiClient, string virtualPurchaseID)
    {
        try
        {
            var purchaseRequest = new PlayerPurchaseVirtualRequest(virtualPurchaseID);

            var response = await gameApiClient.EconomyPurchases.MakeVirtualPurchaseAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId ?? throw new InvalidOperationException("PlayerId is Null"),
                purchaseRequest
            );

            if (response?.Data?.Rewards == null)
            {
                _logger.LogWarning($"[결제] {virtualPurchaseID}의 응답 구조가 올바르지 않습니다.");
            }

            _logger.LogInformation($"[결제 성공] {virtualPurchaseID} 처리 완료");

            // 영수증 데이터를 통째로 반환합니다. (메인 함수에서 가로채기 위해)
            return response!.Data;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, $"[결제 실패] {virtualPurchaseID} : {context.PlayerId}");
            throw;
        }
    }


    #endregion

    #region RealMoney Purchase (현금 결제 / IAP)
    [CloudCodeFunction("ProcessRealMoneyPurchase")]
    public async Task<PlayerEconomyData> ProcessRealMoneyPurchase(IExecutionContext context, IGameApiClient gameApiClient,
        string productId, string receipt, double localPrice, string currencyCode)
    {
        _logger.LogInformation($"[결제 검증 시작] 들어온 productId: {productId ?? "NULL입니다!!"}");
        try
        {

            // 구매 자격 검증 로직 (1회 한정 등)
            await ValidatePlayerEligibility(context, gameApiClient, productId);

            // 영수증 검증 및 보상 지급 영수증을 까보고 구글/애플에 검증을 맡기는 핵심 함수 호출!
            await ProcessStoreReceipt(context, gameApiClient, productId, receipt, localPrice, currencyCode);

            return await _playerEconomyService.GetPlayerEconomyData(context, gameApiClient)
                ?? throw new InvalidOperationException("Failed to get player economy data");

        }
        catch (ApiException ex)
        {
            _logger.LogWarning($"Economy API error processing purchase for product {productId} : {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"UnExpected error processing purchase for product {productId} : {ex.Message}");
            throw;
        }
    }

    // 2. 통합 영수증(Unified Receipt)을 뜯어서 스토어별로 분류하는 함수
    private async Task ProcessStoreReceipt(IExecutionContext context, IGameApiClient gameApiClient,
       string productId, string receipt, double localCost, string localCurrency)
    {
        // 유니티 IAP가 보내준 영수증은 항상 { Store = "어느스토어", Payload = "진짜 영수증 암호문" } 형태입니다.
        var receiptData = JsonConvert.DeserializeAnonymousType(receipt, new { Store = "", Payload = "" })
            ?? throw new JsonException("Unified receipt is null.");

        // 2. 내용물 확인 방어
        // 택배 상자를 열었는데 '어느 스토어(Store)'인지 안 적혀있거나, '알맹이(Payload)'가 없으면 에러를 뿜고 쫓아냅니다.
        if (string.IsNullOrWhiteSpace(receiptData.Store) || string .IsNullOrWhiteSpace(receiptData.Payload))
        {
            throw new JsonException("Unified receipt missing Store/Payload.");
        }

        // 3. 스토어 이름 추출 및 소문자 변환
        // "GooglePlay"를 "googleplay"로 소문자로 맞춰줍니다. (비교하기 쉽게)
        var store = receiptData.Store.ToLowerInvariant();

        switch(store)
        {
            case "fake":
                // 유니티 에디터에서 테스트 결제할 때 타는 로직입니다. 
                // 가짜니까 영수증 검증은 패스하고, 대시보드에 적힌 보상만 그냥 줍니다.

                // UGS의 환경 변수를 읽어와서 라이브 서버면 막아버립니다.
                //if (context.EnvironmentName == "production")
                //{
                //    throw new Exception("라이브 서버에서는 가짜 영수증을 허용하지 않습니다!");
                //}
                _logger.LogInformation("Using fake store - skipping receipt validation");
                await ApplyPurchaseRewardsFromConfiguration(context, gameApiClient, productId);
                break;
            case "googleplay":
                // 안드로이드 폰에서 결제했을 때! Payload(진짜 영수증)를 들고 구글 검증 함수로 갑니다.
                await RedeemGooglePlayPurchase(context, gameApiClient, productId, receiptData.Payload,localCost, localCurrency);
                break;
            case "appleappstore":
                // 애플에서 결제했을 때
                await RedeemAppleAppStorePurchase(context, gameApiClient, productId, receiptData.Payload, localCost, localCurrency);
                break;
            default:
                throw new ArgumentException($"Unsupported store type : {store}");
        }
    }


    // 3. 구글 플레이 영수증 최종 검증 함수
    // 구글 플레이 가로채기(Intercept) 로직 추가
    private async Task RedeemGooglePlayPurchase(IExecutionContext context, IGameApiClient gameApiClient,
       string productId, string googlePayload, double localCost, string currencyCode)
    {
        // 구글의 Payload는 또다시 { json = "데이터", signature = "서명" } 으로 나뉘어 있습니다.
        // Parse the Google-specific payload
        var googleReceipt = JsonConvert.DeserializeAnonymousType(googlePayload, new { json = "", signature = "" })
            ?? throw new JsonException("Failed to parse Google receipt payload.");

        if (string.IsNullOrWhiteSpace(googleReceipt.json) || string.IsNullOrWhiteSpace(googleReceipt.signature))
        {
            throw new JsonException("Google payload missing json/signature");
        }

        // UGS 서버에 보낼 '구글 영수증 검증 요청서'를 만듭니다.
        //var googleRequest = new PlayerPurchaseGoogleplaystoreRequest
        //{
        //    Id = productId,
        //    PurchaseData = googleReceipt.json,
        //    PurchaseDataSignature = googleReceipt.signature,
        //    LocalCost = (int)(localCost* 100),
        //    LocalCurrency = currencyCode,
        //};
        var googleRequest = new PlayerPurchaseGoogleplaystoreRequest(
            id: productId,
            purchaseData: googleReceipt.json,
            purchaseDataSignature: googleReceipt.signature,
            localCost: (int)(localCost * 100),
            localCurrency: currencyCode
            );

        // 구글 검증 & UGS 자동 보상 지급
        // [핵심] UGS 서버야!구글 본사에 이거 진짜인지 물어보고, 진짜면 알아서 보상도 넣어줘!
        var purchaseResult = await gameApiClient.EconomyPurchases.RedeemGooglePlayPurchaseAsync(
            context,
            context.AccessToken,
            context.ProjectId,
            context.PlayerId!,
            googleRequest
            );


        // 현금 결제 후 UGS가 무식하게 지급한 '빈 껍데기'를 부수고 지능형 매니저로 넘깁니다!
        if (purchaseResult.Data?.Rewards?.Inventory != null && purchaseResult.Data.Rewards.Inventory.Count > 0)
        {
            var configResponse = await gameApiClient.EconomyConfiguration.GetPlayerConfigurationAsync(
                context, context.AccessToken, context.ProjectId, context.PlayerId!);

            foreach (var item in purchaseResult.Data.Rewards.Inventory)
            {
                if (item.PlayersInventoryItemIds != null)
                {
                    foreach (var instanceId in item.PlayersInventoryItemIds)
                        await _playerEconomyService.DeleteInventoryItem(context, gameApiClient, instanceId);
                }

                await _playerEconomyService.GrantInventoryItem(context, gameApiClient, item.Id, item.Amount, configResponse.Data.Results);
                await _playerEconomyService.CleanUpNullOrZeroAmountItems(context, gameApiClient, item.Id);
            }
        }

        // [수정됨] 재화(Currency)가 Null이 아닐 때만 안전하게 로그 출력
        if (purchaseResult.Data?.Rewards?.Currency != null)
        {
            foreach (var currency in purchaseResult.Data.Rewards.Currency)
            {
                _logger.LogInformation($"Granted {currency.Amount} {currency.Id}");
            }

        }
        // [수정됨] 인벤토리(Inventory) 아이템이 Null이 아닐 때만 안전하게 로그 출력
        if (purchaseResult.Data?.Rewards?.Inventory != null)
        {
            foreach (var item in purchaseResult.Data.Rewards.Inventory)
            {
                _logger.LogInformation($"Granted {item.Amount}x {item.Id}");
            }
        }
    }
    // 4. 애플 앱스토어 영수증 최종 검증 함수 (구글과 흐름 동일)
    private async Task RedeemAppleAppStorePurchase(IExecutionContext context, IGameApiClient gameApiClient,
       string productId, string applePayload, double localCost, string currencyCode)
    {
       
        if (string.IsNullOrWhiteSpace(applePayload))
        {
            throw new ArgumentException("Apple receipt payload is empty.",nameof(applePayload));
        }

        // 애플은 영수증이 하나로 통일되어 있어 훨씬 심플합니다.
        //var appleRequest = new PlayerPurchaseAppleappstoreRequest
        //{
        //    Id = productId,
        //    Receipt = applePayload,
        //    LocalCost = (int)(localCost * 100),
        //    LocalCurrency = currencyCode,
        //};

        var appleRequest = new PlayerPurchaseAppleappstoreRequest(
            id: productId,
            receipt: applePayload,
            localCost: (int)(localCost * 100),
            localCurrency: currencyCode
        );
        // UGS 서버야, 애플 본사에 물어보고 보상 지급해 줘!
        var purchaseResult = await gameApiClient.EconomyPurchases.RedeemAppleAppStorePurchaseAsync(
            context,
            context.AccessToken,
            context.ProjectId,
            context.PlayerId!,
            appleRequest
            );

        //// 애플 결제 가로채기 로직 (구글과 동일)
        if (purchaseResult.Data?.Rewards?.Inventory != null && purchaseResult.Data.Rewards.Inventory.Count > 0)
        {
            var configResponse = await gameApiClient.EconomyConfiguration.GetPlayerConfigurationAsync(
                context, context.AccessToken, context.ProjectId, context.PlayerId!);

            foreach (var item in purchaseResult.Data.Rewards.Inventory)
            {
                if (item.PlayersInventoryItemIds != null)
                {
                    foreach (var instanceId in item.PlayersInventoryItemIds)
                        await _playerEconomyService.DeleteInventoryItem(context, gameApiClient, instanceId);
                }

                await _playerEconomyService.GrantInventoryItem(context, gameApiClient, item.Id, item.Amount, configResponse.Data.Results);
                await _playerEconomyService.CleanUpNullOrZeroAmountItems(context, gameApiClient, item.Id);
            }
        }
        // [수정됨] 재화(Currency)가 Null이 아닐 때만 안전하게 로그 출력
        if (purchaseResult.Data?.Rewards?.Currency != null)
        {
            foreach (var currency in purchaseResult.Data.Rewards.Currency)
            {
                _logger.LogInformation($"Granted {currency.Amount} {currency.Id}");
            }

        }
        // [수정됨] 인벤토리(Inventory) 아이템이 Null이 아닐 때만 안전하게 로그 출력
        if (purchaseResult.Data?.Rewards?.Inventory != null)
        {
            foreach (var item in purchaseResult.Data.Rewards.Inventory)
            {
                _logger.LogInformation($"Granted {item.Amount}x {item.Id}");
            }
        }
    }

    // 인앱결제 가짜 스토어 테스트용 (Fake)
    public async Task ApplyPurchaseRewardsFromConfiguration(IExecutionContext context, IGameApiClient gameApiClient, string productId)
    {
        try
        {
            // 1. 대시보드 기획서(카탈로그) 가져오기
            // UGS 서버에 접속해서 "현재 우리 게임 상점에 등록된 모든 상품 기획서 좀 줘봐!" 라고 요청합니다.

            var configResponse = await gameApiClient.EconomyConfiguration.GetPlayerConfigurationAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId!
                );

            // 2. 내가 산 상품 찾기
            // 받아온 기획서 뭉치 속에서, 방금 내가 클릭한 상품(productId)만 쏙 뽑아냅니다.
            var realMoneyPurchase = GetRealMoneyPurchaseFromConfig(configResponse.Data.Results, productId);

            // 3. 수동 보상 지급!
            // 그 상품의 기획서에 'Rewards(보상)'가 등록되어 있다면?
            if (realMoneyPurchase?.Rewards != null)
            {
                // 도우미 함수를 시켜서 보상을 내 지갑/인벤토리에 수동으로 꽂아줍니다!
                await DistributeConfiguredRewards(context, gameApiClient, configResponse.Data.Results, realMoneyPurchase.Rewards);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to grant rewards for product {productId}");
            throw;
        }
    }
    // 기획서 목록을 쭉 훑으면서 "이게 현금 상품(RealMoneyPurchaseResource)이 맞는지, 그리고 ID가 똑같은지" 검사해서 상품 정보를 반환합니다.
    private RealMoneyPurchaseResource? GetRealMoneyPurchaseFromConfig(List<PlayerConfigurationResponseResultsInner> results, string productId)
    {
        foreach(var result in results)
        {
            if(result.ActualInstance is RealMoneyPurchaseResource purchase && purchase.Id == productId)
            { 
                return purchase; 
            }

        }
        _logger.LogError($"Real money purchase not found : {productId}");
        throw new InvalidOperationException($"Real money purchase not found : {productId}");
    }
    // 상품 정보 안에 들어있는 보상 목록(예: 골드 100개, 포션 1개)을 반복문(foreach)으로 돌면서, 이게 재화(Currency)인지 인벤토리 아이템인지 분류합니다.
    private async Task DistributeConfiguredRewards(IExecutionContext context, IGameApiClient gameApiClient,
        List<PlayerConfigurationResponseResultsInner> configResults, List<Reward> rewards)
    {
        foreach(var reward in rewards)
        {
            string resourceId = reward.ResourceId;
            int amount = reward.Amount;

            _logger.LogInformation($"Processing reward : {resourceId}, Amount : {amount}");

            string resourceType = _playerEconomyService.GetResourceType(configResults, resourceId);
            await _playerEconomyService.GrantResourceReward(context, gameApiClient, resourceType, resourceId, amount, configResults);
        }
    }

    #endregion

    private async Task ValidatePlayerEligibility(IExecutionContext context, IGameApiClient gameApiClient, string productId)
    {
        // 1. 유저의 인벤토리에서 검사해야 할 '증표(Flag) 아이템 ID'를 담을 변수
        string checkItemKey = string.Empty;

        // 2. 구매하려는 상품 ID(스토어 기준)에 따라 검사할 증표를 짝지어줍니다.
        switch (productId)
        {
            case ServerDefine.k_removeAd: // 스토어에 등록된 '광고 제거' 상품 ID
                checkItemKey = "REMOVE_AD_TICKET";
                break;
            //case "com.mygame.starter_pack_01": // 스토어에 등록된 '초보자 패키지' 상품 ID
            //    checkItemKey = "FLAG_STARTER_PACK_BOUGHT";
            //    break;
            default:
                // 골드나 다이아처럼 무한정 살 수 있는 일반 상품이면 검사 없이 무사통과!
                return;
        }

        // 3. 1회성 상품이라면, 유저 인벤토리를 뒤져서 증표를 이미 가지고 있는지 개수를 물어봅니다.
        int existingAmount = await _playerEconomyService.GetInventoryItemAmount(context, gameApiClient, checkItemKey);

        // 4. 이미 증표를 가지고 있다면? (1개 이상)
        if (existingAmount > 0)
        {
            _logger.LogWarning($"[결제 차단] 유저 {context.PlayerId}는 이미 {productId} 한정 상품을 보유하고 있습니다.");

            // 에러를 던져서 함수를 강제로 폭파시킵니다!
            // 이렇게 하면 아래에 있는 구글/애플 검증(ProcessStoreReceipt)으로 넘어가지 않게 됩니다.
            throw new InvalidOperationException($"Already purchased limited item: {productId}");
        }
    }


    [CloudCodeFunction("ClaimDailyFreeReward")]
    // 리턴 타입을 PlayerEconomyData -> PlayerDataResponse 로 변경
    public async Task<PlayerDataResponse> ClaimDailyFreeReward(IExecutionContext context, IGameApiClient gameApiClient, int amount)
    {
        DateTime kstNow = DateTime.UtcNow.AddHours(9);
        string todayStr = kstNow.ToString("yyyy-MM-dd");

        var (playerExists, playerData) = await _playerDataService.TryGetPlayerData(context, gameApiClient);

        if (!playerExists || playerData == null) throw new InvalidOperationException("PlayerData Not Found");
        if (playerData.LastDailyFreeGoldClaimDate == todayStr) throw new InvalidOperationException("ALREADY_CLAIMED_TODAY");

        // 데이터 갱신 및 저장
        playerData.LastDailyFreeGoldClaimDate = todayStr;
        await _playerDataService.SaveData(context, gameApiClient, ServerDefine.k_PlayerDataKey, playerData);

        // 재화 지급
        await _playerEconomyService.AddCurrency(context, gameApiClient, ServerDefine.k_GoldCurrencyKey, amount);
        var economyData = await _playerEconomyService.GetPlayerEconomyData(context, gameApiClient);

        //  갱신된 플레이어 데이터와 지갑 데이터를 하나로 포장해서 클라이언트로 던져줍니다!
        return new PlayerDataResponse
        {
            PlayerData = playerData,
            PlayerEconomyData = economyData,
            IsNewPlayer = false
        };
    }
}
