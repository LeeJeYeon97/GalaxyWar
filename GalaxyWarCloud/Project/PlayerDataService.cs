using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace Project;

public class PlayerDataService
{
    public const string k_PlayerDataKey = "PLAYER_DATA";
    public const string k_PlayerNameKey = "PLAYER_NAME";

    private PlayerEconomyService _playerEconomyService;
    private static ILogger<PlayerDataService> _logger;

    public PlayerDataService(ILogger<PlayerDataService> logger, PlayerEconomyService playerEconomyService)
    {
        _logger = logger;
        _playerEconomyService = playerEconomyService;
    }

    private async Task SaveData(IExecutionContext context, IGameApiClient gameApiClient, string key, object value)
    {
        try
        {
            await gameApiClient.CloudSaveData.SetItemAsync(
                context, 
                context.AccessToken, 
                context.ProjectId,
                context.PlayerId,
                new SetItemBody(key, value));
        }
        catch (ApiException ex)
        {
            _logger.LogError("Failed to save data. Error: {Error}", ex.Message);
            throw new Exception($"Failed to save data for playerId {context.PlayerId}. Error: {ex.Message}");
        }
    }

    private async Task<object> GetData(IExecutionContext context, IGameApiClient gameApiClient, string key)
    {
        try
        {
            var result = await gameApiClient.CloudSaveData.GetItemsAsync(
                context, 
                context.AccessToken,
                context.ProjectId, 
                context.PlayerId, 
                new List<string> { key });

            // if(result.Data.Results.Count == 0) return null;

            return result.Data.Results.First().Value;
        }
        catch (ApiException ex)
        {
            _logger.LogError("Failed to get data. Error: {Error}", ex.Message);
            throw new Exception($"Failed to get data for playerId {context.PlayerId}. Error: {ex.Message}");
        }
    }

    [CloudCodeFunction("HandlePlayerSignIn")]
    public async Task<PlayerDataResponse> HandlePlayerSignIn(IExecutionContext context, IGameApiClient gameApiClient)
    {
        // 플레이어가 로그인했을 때 데이터 가져오기
        var(playerExists, playerData) = await TryGetPlayerData(context, gameApiClient);

        // 데이터가 없으면 새로운 플레이어
        if ((!playerExists || playerData == null))
        {
            // New Player!
            return await InitializeNewPlayer(context, gameApiClient);
        }

        // 데이터가 있으면 이코노미 데이터도 가져와서 반환하기
        PlayerEconomyData economyData = await _playerEconomyService.GetPlayerEconomyData(context, gameApiClient);

        return new PlayerDataResponse
        {
            PlayerData = playerData,
            PlayerEconomyData = economyData,
            IsNewPlayer = false
        };
    }
    // 플레이어 데이터 가져오기
    private async Task<(bool playerExists, PlayerData? playerData)> TryGetPlayerData(IExecutionContext context, IGameApiClient gameApiClient)
    {
        try
        {
            var (success, playerDataJson) = await TryGetData(context, gameApiClient, k_PlayerDataKey);

            if (playerDataJson == null)
            {
                return (false, null);
            }

            var playerData = JsonConvert.DeserializeObject<PlayerData>($"{playerDataJson}");
            return (playerData != null, playerData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deserializing player data for player : {context.PlayerId}");
            return (false, null);
        }
    }

    public async Task<(bool success, string? value)> TryGetData(IExecutionContext context, IGameApiClient gameApiClient, string key)
    {
        try
        {
            var response = await gameApiClient.CloudSaveData.GetItemsAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId ?? throw new InvalidOperationException("PlayerId is Null"),
                new List<string> { key });


            var retrievedItem = response.Data.Results.FirstOrDefault();
            if (retrievedItem != null)
            {
                return (true, Convert.ToString(retrievedItem.Value));
            }

            return (false, null);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data from CloudSave for player {playerId}", context.PlayerId);
            return (false, null);
        }
        
    }
    // 플레이어 초기화
    private async Task<PlayerDataResponse> InitializeNewPlayer(IExecutionContext context, IGameApiClient gameApiClient)
    {
        PlayerData newPlayerData = new PlayerData
        {
            DisplayName = "New Player",
            Experience = 0,
        };

        PlayerEconomyData newEconomyData;

        try
        {
            // Save new Player Data
            await SaveData(context, gameApiClient, k_PlayerDataKey, newPlayerData);

            // Initialize new Player inventory
            newEconomyData = await _playerEconomyService.InitializeNewPlayerEconomy(context, gameApiClient);

            _logger.LogInformation($"New Player Initialized : {context.PlayerId}");

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to Initialize New Player : {context.PlayerId}");
            throw new Exception("Failed to initialize new player", ex);
        }

        return new PlayerDataResponse
        {
            PlayerData = newPlayerData,
            PlayerEconomyData = newEconomyData,
            IsNewPlayer = true
        };
    }


    private async Task SetRandomNicknameIfEmpty()
    {
       //
       // // 1. 랜덤 숫자 생성 (예: 1000 ~ 9999)
       // int randomNumber = Random.(1000, 10000);
       // string randomName = $"신병{randomNumber}";
       //
       // try
       // {
       //     // 2. 서버에 저장
       //     await AuthenticationService.Instance.UpdatePlayerNameAsync(randomName);
       //     Debug.Log($"임시 닉네임 설정 완료: {randomName}");
       // }
       // catch (Exception ex)
       // {
       //     Debug.LogError($"닉네임 자동 설정 실패: {ex.Message}");
       // }
       
    }
}


