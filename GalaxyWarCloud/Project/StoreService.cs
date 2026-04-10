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

namespace Project
{
    public class StoreService
    {

        private PlayerEconomyService _playerEconomyService;
        private readonly ILogger<StoreService> _logger;

        public StoreService(ILogger<StoreService> logger, PlayerEconomyService playerEconomyService)
        {
            _logger = logger;
            _playerEconomyService = playerEconomyService;
        }

        #region Virtual Purchase

        [CloudCodeFunction("VirtualPurchaseHealthPotion")]
        public async Task<PlayerEconomyData> VirtualPurchaseHealthPotion(IExecutionContext context, IGameApiClient gameApiClient)
        {
            try
            {
                await ProcessVirtualPurchase(context, gameApiClient, ServerDefine.k_HealthPotionPurchaseId);
                // 이거는 왜있는지 확인해보기
                await _playerEconomyService.CleanUpNullOrZeroAmountItems(context, gameApiClient, ServerDefine.k_HealthPotionKey);
                await _playerEconomyService.AddOrUpdateInventoryItemAmount(context, gameApiClient, ServerDefine.k_HealthPotionKey,1);

                return await _playerEconomyService.GetPlayerEconomyData(context, gameApiClient);
            }
            catch(ApiException ex)
            {
                _logger.LogError(ex, $"Failed to purchase potion : {context.PlayerId}");
                throw new Exception($"Failed to purchase potion : {ex.Message}", ex);
            }
        }

        [CloudCodeFunction("PurchaseVirtualItem")]
        public async Task<PlayerEconomyData> PurchaseVirtualItem(IExecutionContext context, IGameApiClient gameApiClient, string purchaseId)
        {
            try
            {
                // 1. 받은 ID(purchaseId)로 UGS 결제 시스템을 돌립니다. (돈 차감 + 아이템 자동 지급 완벽 처리)
                await ProcessVirtualPurchase(context, gameApiClient, purchaseId);

                // 2. 구매가 끝났으니 최신 정보를 넘겨줍니다. (수동 아이템 지급 코드는 삭제함!)
                return await _playerEconomyService.GetPlayerEconomyData(context, gameApiClient);
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, $"Failed to purchase item {purchaseId} for player : {context.PlayerId}");
                throw new Exception($"Failed to purchase item : {ex.Message}", ex);
            }
        }

        private async Task ProcessVirtualPurchase(IExecutionContext context, IGameApiClient gameApiClient, string virtualPurchaseID)
        {
            try
            {
                // "이 ID의 상품을 사고 싶어요" 라는 주문서를 작성합니다.
                var purchaseRequest = new PlayerPurchaseVirtualRequest(virtualPurchaseID);

                // UGS Economy 구매 API를 호출합니다!
                // 여기서 서버가 알아서 내 골드를 깎고, 대시보드에 설정된 보상을 지급해 줍니다.
                var purchaseResponse = await gameApiClient.EconomyPurchases.MakeVirtualPurchaseAsync(
                    context,
                    context.AccessToken,
                    context.ProjectId,
                    context.PlayerId ?? throw new InvalidOperationException("PlayerId is Null"),
                    purchaseRequest
                    );

                // 정상적으로 영수증(Response)이 안 왔다면 방어
                if (purchaseRequest == null || purchaseResponse.Data == null || purchaseResponse.Data.Rewards == null)
                {
                    _logger.LogWarning($"Invalid purchase response structure for {virtualPurchaseID}");
                    return;
                }

                // 결제 성공! 어떤 보상이 지급되었는지 로그로 예쁘게 남깁니다
                List<InventoryExchangeItem> rewardItems = purchaseResponse.Data.Rewards.Inventory;
                _logger.LogInformation($"Virtually purchased : {virtualPurchaseID}. Reward :  " + JsonConvert.SerializeObject(rewardItems));

            }
            catch (ApiException ex)
            {
                // 돈이 없거나, 없는 상품을 사려고 하면 여기서 에러가 잡힙니다.
                _logger.LogError(ex, $"Failed to process potion purchase : {context.PlayerId}");
                throw; // 클라이언트에게 에러를 던져줍니다
            }
        }


        #endregion

        #region RealMoney Purchase
        [CloudCodeFunction("ProcessRealMoneyPurchase")]
        public async Task<PlayerEconomyData> ProcessRealMoneyPurchase(IExecutionContext context, IGameApiClient gameApiClient,
            string productId, string receipt, double localPrice, string currencyCode)
        {
            try
            {
                // 이건 뭐지?
                //await ValidatePlayerEligibility(context, gameApiClient, productId);
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
        
        private async Task ProcessStoreReceipt(IExecutionContext context, IGameApiClient gameApiClient,
           string productId, string receipt, double localCost, string localCurrency)
        {
            var receiptData = JsonConvert.DeserializeAnonymousType(receipt, new { Store = "", Payload = "" })
                ?? throw new JsonException("Unified receipt is null.");

            if(string.IsNullOrWhiteSpace(receiptData.Store) || string .IsNullOrWhiteSpace(receiptData.Payload))
            {
                throw new JsonException("Unified receipt missing Store/Payload.");
            }
            var store = receiptData.Store.ToLowerInvariant();

            switch(store)
            {
                case "fake":
                    _logger.LogInformation("Using fake store - skipping receipt validation");
                    await ApplyPurchaseRewardsFromConfiguration(context, gameApiClient, productId);
                    break;
                case "googleplay":
                    await RedeemGooglePlayPurchase(context, gameApiClient, productId, receiptData.Payload,localCost, localCurrency);
                    break;
                case "appleappstore":
                    await RedeemAppleAppStorePurchase(context, gameApiClient, productId, receiptData.Payload, localCost, localCurrency);
                    break;
                default:
                    throw new ArgumentException($"Unsupported store type : {store}");
            }
        }
        private async Task RedeemGooglePlayPurchase(IExecutionContext context, IGameApiClient gameApiClient,
           string productId, string googlePayload, double localCost, string currencyCode)
        {
            // Parse the Google-specific payload
            var googleReceipt = JsonConvert.DeserializeAnonymousType(googlePayload, new { json = "", signature = "" })
                ?? throw new JsonException("Failed to parse Google receipt payload.");

            if (string.IsNullOrWhiteSpace(googleReceipt.json) || string.IsNullOrWhiteSpace(googleReceipt.signature))
            {
                throw new JsonException("Google payload missing json/signature");
            }

            var googleRequest = new PlayerPurchaseGoogleplaystoreRequest
            {
                Id = productId,
                PurchaseData = googleReceipt.json,
                PurchaseDataSignature = googleReceipt.signature,
                LocalCost = (int)(localCost* 100),
                LocalCurrency = currencyCode,
            };

            var purchaseResult = await gameApiClient.EconomyPurchases.RedeemGooglePlayPurchaseAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId!,
                googleRequest
                );

            foreach(var currency in purchaseResult.Data.Rewards.Currency)
            {
                _logger.LogInformation($"Granted {currency.Amount} {currency.Id}");
            }

            foreach(var item in purchaseResult.Data.Rewards.Inventory)
            {
                _logger.LogInformation($"Granted {item.Amount}x {item.Id}");
            }
        }
        private async Task RedeemAppleAppStorePurchase(IExecutionContext context, IGameApiClient gameApiClient,
           string productId, string applePayload, double localCost, string currencyCode)
        {
           
            if (string.IsNullOrWhiteSpace(applePayload))
            {
                throw new ArgumentException("Apple receipt payload is empty.",nameof(applePayload));
            }


            var appleRequest = new PlayerPurchaseAppleappstoreRequest
            {
                Id = productId,
                Receipt = applePayload,
                LocalCost = (int)(localCost * 100),
                LocalCurrency = currencyCode,
            };

            var purchaseResult = await gameApiClient.EconomyPurchases.RedeemAppleAppStorePurchaseAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId!,
                appleRequest
                );

            foreach (var currency in purchaseResult.Data.Rewards.Currency)
            {
                _logger.LogInformation($"Granted {currency.Amount} {currency.Id}");
            }

            foreach (var item in purchaseResult.Data.Rewards.Inventory)
            {
                _logger.LogInformation($"Granted {item.Amount}x {item.Id}");
            }
        }
        public async Task ApplyPurchaseRewardsFromConfiguration(IExecutionContext context, IGameApiClient gameApiClient, string productId)
        {
            try
            {
                var configResponse = await gameApiClient.EconomyConfiguration.GetPlayerConfigurationAsync(
                    context,
                    context.AccessToken,
                    context.ProjectId,
                    context.PlayerId!
                    );
                var realMoneyPurchase = GetRealMoneyPurchaseFromConfig(configResponse.Data.Results, productId);

                if (realMoneyPurchase?.Rewards != null)
                {
                    await DistributeConfiguredRewards(context, gameApiClient, configResponse.Data.Results, realMoneyPurchase.Rewards);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to grant rewards for product {productId}");
                throw;
            }
        }
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
        private async Task DistributeConfiguredRewards(IExecutionContext context, IGameApiClient gameApiClient,
            List<PlayerConfigurationResponseResultsInner> configResults, List<Reward> rewards)
        {
            foreach(var reward in rewards)
            {
                string resourceId = reward.ResourceId;
                int amount = reward.Amount;

                _logger.LogInformation($"Processing reward : {resourceId}, Amount : {amount}");

                string resourceType = _playerEconomyService.GetResourceType(configResults, resourceId);
                await _playerEconomyService.GrantResourceReward(context, gameApiClient,resourceType,resourceId, amount);
            }
        }

        #endregion
    }
}
