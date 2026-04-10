using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.Economy.Model;

namespace Project
{
    public class PlayerEconomyService
    {
        private readonly ILogger<PlayerEconomyService> _logger;

        public PlayerEconomyService(ILogger<PlayerEconomyService> logger)
        {
            _logger = logger;
        }

        private async Task<int> GetPlayerGold(IExecutionContext context, IGameApiClient gameApiClient)
        {
            return await GetCurrencyAmount(context, gameApiClient, ServerDefine.k_GoldCurrencyKey);
        }        
        private async Task<int> GetHealthPotionAmount(IExecutionContext context, IGameApiClient gameApiClient)
        {
            return await GetInventoryItemAmount(context, gameApiClient, ServerDefine.k_HealthPotionKey);
        }

        [CloudCodeFunction("GetPlayerEconomyData")]
        public async Task<PlayerEconomyData> GetPlayerEconomyData(IExecutionContext context, IGameApiClient gameApiClient)
        {
            try
            {
                var economyData = new PlayerEconomyData();

                int goldAmount = await GetPlayerGold(context, gameApiClient);
                economyData.Currencies[ServerDefine.k_GoldCurrencyKey] = goldAmount;

                // Add any other currencies here..

                // Get Player inventory and add to economy data
                economyData.ItemInventory = await GetPlayerInventoryItemAmountMap(context, gameApiClient);

                return economyData;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to sync economy data : {PlayerId}", context.PlayerId);
                throw new Exception($"Failed to sync economy : {ex.Message}", ex);
            }
        }

        [CloudCodeFunction("AddHealthPotion")]
        public async Task AddHealthPotion(IExecutionContext context, IGameApiClient gameApiClient)
        {
            await AddNewInventoryItem(context, gameApiClient, ServerDefine.k_HealthPotionKey, 1);
        }
        private async Task<List<InventoryResponse>> GetPlayerInventory(IExecutionContext context, IGameApiClient gameApiClient, int? limit = null, params string[]? inventoryItemIds)
        {
            try
            {
                List<string>? ids = inventoryItemIds?.Length > 0 ? inventoryItemIds.ToList() : null;

                // Call the API to get player inventory
                var playerInventory = await gameApiClient.EconomyInventory.GetPlayerInventoryAsync(
                    context,
                    context.AccessToken,
                    context.ProjectId,
                    context.PlayerId!,
                    inventoryItemIds: ids,
                    limit: limit
                    );
                return playerInventory.Data.Results;
            }
            catch (ApiException ex)
            {
                _logger.LogError($"Failed to get inventory for player{context.PlayerId}. Error : {ex.Message}");
                throw new Exception($"Failed to get inventory : {ex.Message}", ex);
            }
        }
        private async Task<Dictionary<string,int>> GetPlayerInventoryItemAmountMap(IExecutionContext context, IGameApiClient gameApiClient, params string[]? inventoryItemIds)
        {
            var items = await GetPlayerInventory(context, gameApiClient, inventoryItemIds: inventoryItemIds);

            return items.Where(item => !string.IsNullOrEmpty(item.InventoryItemId))
                .ToDictionary
                (
                    item => item.InventoryItemId!,
                    item => GetInventoryItemCustomData<int?>(item, "amount") ?? 1
                );
        }
        private T? GetInventoryItemCustomData<T>(InventoryResponse item, string key)
        {
            if (item?.InstanceData == null) return default;

            try
            {
                // Convert to JObject if it isn't already
                var jObject = item.InstanceData as Newtonsoft.Json.Linq.JObject
                    ?? Newtonsoft.Json.Linq.JObject.Parse(item.InstanceData?.ToString() ?? "{}");

                // Get Value using indexer syntax
                var token = jObject[key];
                if (token != null)
                {
                    return token.ToObject<T>();
                }

            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get {key} from item {item.InventoryItemId} : {ex.Message}");
            }
            return default;
        }
        public async Task<PlayerEconomyData> InitializeNewPlayerEconomy(IExecutionContext context, IGameApiClient gameApiClient)
        {
            await InitializeInventory(context, gameApiClient);
            return await GetPlayerEconomyData(context, gameApiClient);
        }
        private async Task InitializeInventory(IExecutionContext context, IGameApiClient gameApiClient)
        {
            var startingItems = new Dictionary<string, int>
            {
                //{k_HealthPotionKey,1 },
            };

            foreach (var item in startingItems)
            {
                try
                {
                    await AddNewInventoryItem(context, gameApiClient, item.Key, item.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to grant initial inventory item {item.Key}");
                }
            }
        }
        private async Task<int> GetCurrencyAmount(IExecutionContext context, IGameApiClient gameApiClient, string key)
        {
            try
            {
                var playerCurrenciesData = await gameApiClient.EconomyCurrencies.GetPlayerCurrenciesAsync(
                    context,
                    context.AccessToken,
                    context.ProjectId,
                    context.PlayerId!
                    );

                CurrencyBalanceResponse? targetCurrency = playerCurrenciesData.Data.Results.FirstOrDefault(currency => currency.CurrencyId == key);

                if (targetCurrency != null)
                {
                    return (int)targetCurrency.Balance;
                }
                else
                {
                    throw new Exception($"Currency {key} not found");
                }
            }
            catch (ApiException ex)
            {
                throw new Exception($"Failed to get currency {key} for player {context.PlayerId}. Error : {ex.Message}");
            }
        }

        private async Task<int> GetInventoryItemAmount(IExecutionContext context, IGameApiClient gameApiClient, string key)
        {
            try
            {
                var inventoryResponse = await gameApiClient.EconomyInventory.GetPlayerInventoryAsync(
                    context,
                    context.AccessToken,
                    context.ProjectId,
                    context.PlayerId!,
                    playersInventoryItemIds: new List<string> { key }
                    );

                InventoryResponse? item = inventoryResponse.Data.Results.FirstOrDefault();

                // 수정 1: 아이템이 없을 경우 로그를 남기고 0을 반환합니다.
                if (item == null)
                {
                    _logger.LogInformation($"Inventory item {key} not found for player '{context.PlayerId}'");
                    return 0;
                }

                // 수정 2: 파싱에 성공했다면, 꺼내온 amount 값을 반환합니다.
                if (TryParseInventoryItemAmount(item, out int amount))
                {
                    return amount;
                }

                // 수정 3: 파싱에 실패했을 경우 안전하게 0을 반환합니다.
                return 0;
            }
            catch (ApiException ex)
            {
                throw new Exception($"Failed to get inventory item {key} for player {context.PlayerId}. Error : {ex.Message}");
            }
        }
        private bool TryParseInventoryItemAmount(InventoryResponse itemResponse, out int amount)
        {
            amount = 0;

            if(itemResponse.InstanceData == null)
            {
                _logger.LogWarning($"Item '{itemResponse.InventoryItemId}' instance data is null");
                return false;
            }

            try
            {
                string json = $"{itemResponse.InstanceData}";
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                if (data != null && data.TryGetValue("amount", out var amountObj))
                {
                    if (int.TryParse(amountObj.ToString(), out amount))
                    {
                        return true;
                    }
                    _logger.LogWarning($"Amount value '{amountObj}' for '{itemResponse.InventoryItemId}' is not a valid integer");
                    return false;
                }
                _logger.LogWarning($"Instance data for '{itemResponse.InventoryItemId}' doesn't contain an 'amount' property");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to parse inventory item amount for '{itemResponse.InventoryItemId}' : {ex.Message}");
                return false;
            }
            return false;
        }

        public async Task AddNewInventoryItem(IExecutionContext context, IGameApiClient gameApiClient, string itemId, int amount)
        {
            var instanceData = new Dictionary<string, object>
            {
                {"amount", amount }
            };

            await AddNewInventoryItem(context, gameApiClient, itemId, instanceData); 
        }

        public async Task AddNewInventoryItem(IExecutionContext context, IGameApiClient gameApiClient, string itemId, Dictionary<string,object> instanceData)
        {
            var inventoryRequest = new AddInventoryRequest(itemId, instanceData: instanceData);

            try
            {
                await gameApiClient.EconomyInventory.AddInventoryItemAsync(
                    context,
                    context.AccessToken,
                    context.ProjectId,
                    context.PlayerId ?? throw new InvalidOperationException("PlayerId is null"),
                    inventoryRequest
                    );
            }
            catch (ApiException ex)
            {
                _logger.LogError($"Failed to add inventory item {itemId} for player {context.PlayerId}. Error : {ex}");


            }
        }

        public async Task CleanUpNullOrZeroAmountItems(IExecutionContext context, IGameApiClient gameApiClient, string itemKey)
        {
            try
            {
                var items = await GetPlayerInventory(context, gameApiClient, inventoryItemIds: new string[] { itemKey });

                var itemsToDelete = new List<string>();

                foreach(var item in items)
                {
                    if (string.IsNullOrEmpty(item.PlayersInventoryItemId)) continue;

                    // check for null instance data
                    if(item.InstanceData == null)
                    {
                        itemsToDelete.Add(item.PlayersInventoryItemId);
                        _logger.LogInformation($"Found {itemKey} with null instance data : {item.PlayersInventoryItemId}");
                        continue;
                    }

                    if(!TryParseInventoryItemAmount(item, out int amount))
                    {
                        continue;
                    }

                    if(amount <= 0)
                    {
                        itemsToDelete.Add(item.PlayersInventoryItemId);
                    }
                }
                foreach(var itemId in itemsToDelete)
                {
                    await DeleteInventoryItem(context, gameApiClient, itemId);
                    _logger.LogInformation($"Deleted zero-amount {itemId}");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Failed to clean up zero-amount {itemKey} for player {context.PlayerId}");
            }
        }
        public async Task DeleteInventoryItem(IExecutionContext context, IGameApiClient gameApiClient, string inventoryItemId)
        {
            await gameApiClient.EconomyInventory.DeleteInventoryItemAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId ?? throw new InvalidOperationException("PlayerId is Null"),
                inventoryItemId
                );

        }
        public async Task AddOrUpdateInventoryItemAmount(IExecutionContext context, IGameApiClient gameApiClient, string itemKey, int amountToAdd,
            Dictionary<string, object>? customData = null) // Optional parameter for other custom data
        {
            var inventoryItems = await GetPlayerInventory(context, gameApiClient, inventoryItemIds: new string[] { itemKey });
            InventoryResponse? existingItem = inventoryItems.FirstOrDefault(item => !string.IsNullOrEmpty(item.PlayersInventoryItemId));

            bool itemExistsInInventory = existingItem != null;

            // Determine amount to use
            int totalAmount = amountToAdd;
            if(itemExistsInInventory)
            {
                TryParseInventoryItemAmount(existingItem!, out int currentAmount); // Defaults to 0 if parsing fails

                totalAmount = currentAmount + amountToAdd;
            }

            // Prepare instance data with amount
            var instanceData = new Dictionary<string, object>
            {
                {"amount", totalAmount }
            };

            // Add any custom data provided
            if(customData != null)
            {
                foreach(var kvp in customData )
                {
                    instanceData[kvp.Key] = kvp.Value;
                }
            }

            if(itemExistsInInventory)
            {
                var updateRequest = new InventoryRequestUpdate(instanceData: instanceData);
                await gameApiClient.EconomyInventory.UpdateInventoryItemAsync(
                    context,
                    context.AccessToken,
                    context.ProjectId,
                    context.PlayerId!,
                    existingItem!.PlayersInventoryItemId!,
                    updateRequest
                    );
            }
            else
            {
                // Create new item with data
                await AddNewInventoryItem(context, gameApiClient, itemKey, instanceData);
            }
        }

        public string GetResourceType(List<PlayerConfigurationResponseResultsInner> results, string resourceId)
        {
            foreach(var result in results)
            {
                switch(result.ActualInstance)
                {
                    case CurrencyResource currency when currency.Id == resourceId:
                        return "CURRENCY";
                    case InventoryItemResource item when item.Id == resourceId:
                        return "INVENTORY_ITEM";
                    case RealMoneyPurchaseResource purchase when purchase.Id == resourceId:
                        return "REAL_MONEY_PURCHASE";
                    case VirtualPurchaseResource virtualPurchase when virtualPurchase.Id == resourceId:
                        return "VIRTUAL_PURCHASE";
                }
            }
            return "UNKNOWN";
        }
        public async Task GrantResourceReward(IExecutionContext context, IGameApiClient gameApiClient, string resourceType, string resourceId, int amount)
        {
            switch(resourceType)
            {
                case "CURRENCY":
                    await AddCurrency(context, gameApiClient, resourceId, amount);
                    _logger.LogInformation($"Added currency : {resourceId}, Amount : {amount}");
                    break;
                case "INVENTORY_ITEM":
                    await AddOrUpdateInventoryItemAmount(context, gameApiClient, resourceId, amount);
                    _logger.LogInformation($"Added inventory item : {resourceId}, Amount : {amount}");
                    break;
                default:
                    _logger.LogWarning($"Unknown resource type for reward : {resourceType}");
                    break;
            }
        }

        private async Task AddCurrency(IExecutionContext context, IGameApiClient gameApiClient, string resourceId, int amount)
        {
            try
            {
                // 1. 증가시킬 수량을 담은 요청(Request) 객체를 만듭니다.
                // 참고: 만약 amount가 음수(-500)라면 알아서 차감됩니다!
                var modifyBalanceRequest = new CurrencyModifyBalanceRequest(resourceId, amount);

                // 2. UGS 서버에 해당 재화의 잔액을 변경해달라고 API를 호출합니다.
                await gameApiClient.EconomyCurrencies.IncrementPlayerCurrencyBalanceAsync(
                    context,
                    context.AccessToken,
                    context.ProjectId,
                    context.PlayerId!,
                    resourceId,
                    modifyBalanceRequest
                );

                _logger.LogInformation($"Successfully added {amount} to currency {resourceId} for player {context.PlayerId}");
            }
            catch (ApiException ex)
            {
                // 에러가 발생하면 로그를 남기고 클라이언트 쪽에 예외를 던집니다.
                _logger.LogError(ex, $"Failed to add {amount} to currency {resourceId} for player {context.PlayerId}. Error: {ex.Message}");
                throw new Exception($"Failed to add currency {resourceId}: {ex.Message}", ex);
            }
        }
    }
}
