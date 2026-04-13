using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.Economy.Model;

namespace Project;

public class PlayerEconomyService
{
    private readonly ILogger<PlayerEconomyService> _logger;

    public PlayerEconomyService(ILogger<PlayerEconomyService> logger)
    {
        _logger = logger;
    }

    #region [1] 인벤토리 초기화 함수
    // 새로운 유저
    public async Task<PlayerEconomyData> InitializeNewPlayerEconomy(IExecutionContext context, IGameApiClient gameApiClient)
    {
        await InitializeInventory(context, gameApiClient);
        return await GetPlayerEconomyData(context, gameApiClient);
    }
    private async Task InitializeInventory(IExecutionContext context, IGameApiClient gameApiClient)
    {
        // 시작 아이템 필요 시 추가
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

    #endregion

    //[핵심] 클라이언트가 게임에 접속하거나 상점에서 물건을 산 뒤 "내 최신 정보 좀 줘!" 할 때 부르는 함수
    [CloudCodeFunction("GetPlayerEconomyData")]
    public async Task<PlayerEconomyData> GetPlayerEconomyData(IExecutionContext context, IGameApiClient gameApiClient)
    {
        try
        {
            var economyData = new PlayerEconomyData();

            
            // 1. 재화 (골드 등)
            int goldAmount = await GetCurrencyAmount(context, gameApiClient, ServerDefine.k_GoldCurrencyKey);
            economyData.Currencies[ServerDefine.k_GoldCurrencyKey] = goldAmount;

            // Add any other currencies here..

            // 2. 인벤토리 싹 다 가져오기 (Limit 100)
            var rawItems = await GetPlayerInventory(context, gameApiClient, limit: 100);

            // [핵심 변경] 기획서를 긁어와서 이게 장비인지 소모품인지 구별할 준비를 합니다!
            var configResponse = await gameApiClient.EconomyConfiguration.GetPlayerConfigurationAsync(
                context, context.AccessToken, context.ProjectId, context.PlayerId!);

            foreach (var item in rawItems)
            {
                if (string.IsNullOrEmpty(item.InventoryItemId)) continue;

                string itemKey = item.InventoryItemId;
                bool isStackable = CheckIfItemIsStackable(configResponse.Data.Results, itemKey);

                // 장비 (독립형) -> 상세 리스트에 개별 추가!
                if (!isStackable)
                {
                    var equipData = new EquipmentItemData
                    {
                        InstanceId = item.PlayersInventoryItemId!, // 고유 일련번호!
                        ItemKey = itemKey,
                        Amount = GetInventoryItemCustomData<int?>(item, "amount") ?? 1,
                        Level = GetInventoryItemCustomData<int?>(item, "level") ?? 0 // 레벨 파싱!
                    };
                    economyData.EquipmentList.Add(equipData);
                }
                // 소모품 (스택형) -> 딕셔너리에 수량 합산!
                else
                {
                    int amount = GetInventoryItemCustomData<int?>(item, "amount") ?? 1;
                    if (economyData.ItemInventory.ContainsKey(itemKey))
                    {
                        economyData.ItemInventory[itemKey] += amount; // 합치기
                    }
                    else
                    {
                        economyData.ItemInventory[itemKey] = amount; // 새로 넣기
                    }
                }
            }

            return economyData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync economy data : {PlayerId}", context.PlayerId);
            throw new Exception($"Failed to sync economy : {ex.Message}", ex);
        }
    }

    [CloudCodeFunction("GetInventoryItemAmount")]
    public async Task<int> GetInventoryItemAmount(IExecutionContext context, IGameApiClient gameApiClient, string key)
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


    #region [2] 재화(Currency) 관리 로직 (골드, 다이아 등)

    // 특정 재화(예: 골드)의 현재 잔액을 UGS 서버에 물어보는 함수
    private async Task<int> GetCurrencyAmount(IExecutionContext context, IGameApiClient gameApiClient, string key)
    {
        try
        {
            // UGS API 호출: 이 유저의 지갑(Currencies)을 전부 가져옵니다.
            var playerCurrenciesData = await gameApiClient.EconomyCurrencies.GetPlayerCurrenciesAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId!
                );

            // 내가 찾고 싶은 재화(key)만 쏙 뽑아냅니다
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
    // 특정 재화의 숫자를 올리거나 내리는(음수 입력 시) 함수

    public async Task AddCurrency(IExecutionContext context, IGameApiClient gameApiClient, string resourceId, int amount)
    {
        try
        {
            // 1. 증가시킬 수량을 담은 요청(Request) 객체를 만듭니다.
            // 참고: 만약 amount가 음수(-500)라면 알아서 차감됩니다!
            // UGS에 보낼 영수증(요청서) 작성
            var modifyBalanceRequest = new CurrencyModifyBalanceRequest(resourceId, amount);

            // 2. UGS 서버에 해당 재화의 잔액을 변경해달라고 API를 호출합니다.
            // API 호출: 잔액 변경! (UGS가 알아서 기존 금액에 더하거나 뺍니다)
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
    #endregion

    #region [3] 인벤토리(Inventory) 조회 및 파싱 로직
    // UGS 서버에서 유저의 인벤토리 목록을 가져오는 베이스 함수
    // limit: 한 번에 몇 개까지 가져올지 / inventoryItemIds: 특정 아이템만 검색할지
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

    // 인벤토리를 싹 긁어와서 [아이템ID : 수량(amount)] 형태의 깔끔한 딕셔너리로 만들어주는 함수
    private async Task<Dictionary<string,int>> GetPlayerInventoryItemAmountMap(IExecutionContext context, IGameApiClient gameApiClient, params string[]? inventoryItemIds)
    {

        // 페이징 버그 해결: 특정 ID 조회가 아니라면 한 번에 최대치(100개)를 가져오도록 limit 설정
        int fetchLimit = (inventoryItemIds == null || inventoryItemIds.Length == 0) ? 100 : 20;
        var items = await GetPlayerInventory(context, gameApiClient, limit: fetchLimit, inventoryItemIds: inventoryItemIds);

        //var items = await GetPlayerInventory(context, gameApiClient, inventoryItemIds: inventoryItemIds);

        // C# LINQ를 사용해 리스트를 딕셔너리로 예쁘게 변환합니다.
        return items.Where(item => !string.IsNullOrEmpty(item.InventoryItemId))
            .GroupBy(item => item.InventoryItemId!)
            .ToDictionary
            (
                group => group.Key, // 아이템 ID (예: "TEST_EQUIP")
                group => group.Sum(item => GetInventoryItemCustomData<int?>(item, "amount") ?? 1) // 그룹 내 모든 amount를 합산
            );
    }

    // [기술 핵심] UGS의 InstanceData(JSON)를 파싱해서 원하는 키(예: "amount")의 값을 빼오는 만능 도우미
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
    #endregion

    #region [4] 인벤토리 수정(스택, 생성, 삭제) 로직

    // [핵심 변경 1] 아이템 지급의 메인 매니저 함수 (기존 AddOrUpdate... 를 한 겹 감쌈)
    public async Task GrantInventoryItem(IExecutionContext context, IGameApiClient gameApiClient, string itemKey, int amount, List<PlayerConfigurationResponseResultsInner> configResults)
    {
        // 1. UGS 기획서에서 이 아이템이 스택(물약)인지, 독립형(장비)인지 확인
        bool isStackable = CheckIfItemIsStackable(configResults, itemKey);

        if (isStackable)
        {
            // [소모품] 스택 로직 실행 (안전한 병합)
            await AddOrUpdateInventoryItemAmount(context, gameApiClient, itemKey, amount);
        }
        else
        {
            // [장비/펫] 동적 도면 복사 후 새 슬롯 생성
            var itemConfig = configResults
                .Select(r => r.ActualInstance as InventoryItemResource)
                .FirstOrDefault(item => item != null && item.Id == itemKey);

            var blueprintData = GetInitialDataFromConfig(itemConfig);

            for (int i = 0; i < amount; i++)
            {
                var instanceData = new Dictionary<string, object>(blueprintData);
                instanceData["amount"] = 1; // 장비는 무조건 1개

                await AddNewInventoryItem(context, gameApiClient, itemKey, instanceData);
            }
            _logger.LogInformation($"[독립형 생성] {itemKey} 아이템 {amount}개 생성 완료");
        }
    }

    // 기존 데이터를 날리지 않고 안전하게 'amount'만 덮어쓰는 병합 로직
    public async Task AddOrUpdateInventoryItemAmount(IExecutionContext context, IGameApiClient gameApiClient, string itemKey, int amountToAdd,
        Dictionary<string, object>? customData = null) // Optional parameter for other custom data
    {
        // 1. 이미 인벤토리에 이 아이템이 있는지 찾아봅니다.
        var inventoryItems = await GetPlayerInventory(context, gameApiClient, inventoryItemIds: new string[] { itemKey });
        InventoryResponse? existingItem = inventoryItems.FirstOrDefault(item => !string.IsNullOrEmpty(item.PlayersInventoryItemId));

        bool itemExistsInInventory = existingItem != null;

        // Determine amount to use

        // 2. 이미 있다면? 기존 개수에 방금 산 개수를 더합니다.
        int totalAmount = amountToAdd;

        var instanceData = new Dictionary<string, object>();

        if (itemExistsInInventory)
        {
            TryParseInventoryItemAmount(existingItem!, out int currentAmount); // Defaults to 0 if parsing fails

            totalAmount = currentAmount + amountToAdd;

            // 기존 데이터(레벨, 내구도 등) 보존을 위한 병합 작업
            if (existingItem!.InstanceData != null)
            {
                try
                {
                    var existingJson = existingItem.InstanceData.ToString();
                    var existingDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(existingJson!);
                    if (existingDict != null)
                    {
                        foreach (var kvp in existingDict) instanceData[kvp.Key] = kvp.Value;
                    }
                }
                catch { _logger.LogWarning("InstanceData 병합 실패"); }
            }
        }

        // amount 값만 덮어쓰기
        instanceData["amount"] = totalAmount;

        // Add any custom data provided
        if (customData != null)
        {
            foreach (var kvp in customData)
            {
                instanceData[kvp.Key] = kvp.Value;
            }
        }

        // 4. 이미 있던 아이템이면 Update(수정)하고, 없던 아이템이면 Add(새로 생성)합니다.
        if (itemExistsInInventory)
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

    public async Task AddNewInventoryItem(IExecutionContext context, IGameApiClient gameApiClient, string itemId, Dictionary<string, object> instanceData)
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
    public async Task AddNewInventoryItem(IExecutionContext context, IGameApiClient gameApiClient, string itemId, int amount)
    {
        var instanceData = new Dictionary<string, object>
        {
            {"amount", amount }
        };

        await AddNewInventoryItem(context, gameApiClient, itemId, instanceData);
    }

    // 인벤토리 아이템 삭제
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
    // 아이템의 개수가 0개 이하이거나 데이터가 깨진 '쓰레기 슬롯'을 찾아서 지워주는 청소 함수
    public async Task CleanUpNullOrZeroAmountItems(IExecutionContext context, IGameApiClient gameApiClient, string itemKey)
    {
        try
        {
            var items = await GetPlayerInventory(context, gameApiClient, inventoryItemIds: new string[] { itemKey });

            var itemsToDelete = new List<string>();

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.PlayersInventoryItemId)) continue;

                // check for null instance data
                if (item.InstanceData == null)
                {
                    itemsToDelete.Add(item.PlayersInventoryItemId);
                    _logger.LogInformation($"Found {itemKey} with null instance data : {item.PlayersInventoryItemId}");
                    continue;
                }

                if (!TryParseInventoryItemAmount(item, out int amount))
                {
                    continue;
                }

                if (amount <= 0)
                {
                    itemsToDelete.Add(item.PlayersInventoryItemId);
                }
            }
            foreach (var itemId in itemsToDelete)
            {
                await DeleteInventoryItem(context, gameApiClient, itemId);
                _logger.LogInformation($"Deleted zero-amount {itemId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to clean up zero-amount {itemKey} for player {context.PlayerId}");
        }
    }

    private bool TryParseInventoryItemAmount(InventoryResponse itemResponse, out int amount)
    {
        amount = 0;

        if (itemResponse.InstanceData == null)
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

    #endregion


    #region [5] 보상 지급 라우터
    public string GetResourceType(List<PlayerConfigurationResponseResultsInner> results, string resourceId)
    {
        foreach (var result in results)
        {
            switch (result.ActualInstance)
            {
                case CurrencyResource currency when currency.Id == resourceId: return "CURRENCY";
                case InventoryItemResource item when item.Id == resourceId: return "INVENTORY_ITEM";
                case RealMoneyPurchaseResource purchase when purchase.Id == resourceId: return "REAL_MONEY_PURCHASE";
                case VirtualPurchaseResource virtualPurchase when virtualPurchase.Id == resourceId: return "VIRTUAL_PURCHASE";
            }
        }
        return "UNKNOWN";
    }

    // [핵심 변경 3] 이제 InventoryItem은 분기 처리가 포함된 GrantInventoryItem으로 토스합니다.
    public async Task GrantResourceReward(IExecutionContext context, IGameApiClient gameApiClient, string resourceType, string resourceId, int amount, List<PlayerConfigurationResponseResultsInner> configResults)
    {
        switch (resourceType)
        {
            case "CURRENCY":
                await AddCurrency(context, gameApiClient, resourceId, amount);
                break;
            case "INVENTORY_ITEM":
                // 인벤토리 아이템은 스택/장비 판별을 위해 configResults를 넘겨줍니다.
                await GrantInventoryItem(context, gameApiClient, resourceId, amount, configResults);
                break;
            default:
                _logger.LogWarning($"Unknown resource type : {resourceType}");
                break;
        }
    }
    #endregion

    // [동적 데이터 추출] 기획서에서 InitialData 도면 추출
    private Dictionary<string, object> GetInitialDataFromConfig(InventoryItemResource? itemConfig)
    {
        var initialData = new Dictionary<string, object>();

        // 여기도 CustomData를 파싱하도록 수정합니다.
        if (itemConfig?.CustomData != null)
        {
            try
            {
                // 1. CustomData 전체를 딕셔너리로 변환
                string json = itemConfig.CustomData.ToString() ?? "{}";
                var customDataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                // 2. 그 안에서 "InitialData"라는 이름의 덩어리를 찾음
                if (customDataDict != null && customDataDict.TryGetValue("InitialData", out var initialDataObj))
                {
                    // 3. InitialData 덩어리를 다시 한번 딕셔너리로 변환해서 반환!
                    var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(initialDataObj.ToString() ?? "{}");
                    if (dataDict != null) return dataDict;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"InitialData 파싱 실패: {ex.Message}");
            }
        }
        return initialData;
    }

    // [동적 데이터 추출] 기획서에서 IsStackable 값 확인
    private bool CheckIfItemIsStackable(List<PlayerConfigurationResponseResultsInner> configResults, string itemKey)
    {
        var itemConfig = configResults
             .Select(r => r.ActualInstance as InventoryItemResource)
             .FirstOrDefault(item => item != null && item.Id == itemKey);

        // CustomDataDeserialized 대신 CustomData를 확인합니다.
        if (itemConfig?.CustomData != null)
        {
            try
            {
                // 1. object로 넘어온 CustomData를 JSON 문자열로 바꾼 뒤 딕셔너리로 파싱합니다.
                string json = itemConfig.CustomData.ToString() ?? "{}";
                var customDataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                // 2. 딕셔너리에서 IsStackable 값을 찾습니다.
                if (customDataDict != null && customDataDict.TryGetValue("IsStackable", out var isStackableObj))
                {
                    if (bool.TryParse(isStackableObj.ToString(), out bool result)) return result;
                }
            }
            catch
            {
                /* 파싱 실패 시 조용히 넘어갑니다 (기본값 false 반환) */
            }
        }
        return false; // 기본값은 스택 불가(장비)
    }
}
