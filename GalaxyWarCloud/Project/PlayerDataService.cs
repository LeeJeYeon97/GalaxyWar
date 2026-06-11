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
    private PlayerEconomyService _playerEconomyService;
    private readonly ILogger<PlayerDataService> _logger;

    public PlayerDataService(ILogger<PlayerDataService> logger, PlayerEconomyService playerEconomyService)
    {
        _logger = logger;
        _playerEconomyService = playerEconomyService;
    }

    public async Task SaveData(IExecutionContext context, IGameApiClient gameApiClient, string key, object value)
    {
        try
        {
            await gameApiClient.CloudSaveData.SetItemAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId!,
                new SetItemBody(key, value));
        }
        catch (ApiException ex)
        {
            _logger.LogError("Failed to save data. Error: {Error}", ex.Message);
            throw new Exception($"Failed to save data for playerId {context.PlayerId}. Error: {ex.Message}");
        }
    }

    public async Task SaveData(IExecutionContext context, IGameApiClient gameApiClient, string key, string value)
    {
        try
        {
            await gameApiClient.CloudSaveData.SetItemAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                context.PlayerId!,
                new SetItemBody(key, value));
        }
        catch (ApiException ex)
        {
            _logger.LogError("Failed to save data. Error: {Error}", ex.Message);
            throw new Exception($"Failed to save data for playerId {context.PlayerId}. Error: {ex.Message}");
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data from CloudSave for player {playerId}", context.PlayerId);
            return (false, null);
        }

    }

    
    // 로그인 할때 클라에서 부르는 함수
    [CloudCodeFunction("HandlePlayerSignIn")]
    public async Task<PlayerDataResponse> HandlePlayerSignIn(IExecutionContext context, IGameApiClient gameApiClient, string authPlayerName)
    {
        // 플레이어가 로그인했을 때 데이터 가져오기
        var (playerExists, playerData) = await TryGetPlayerData(context, gameApiClient);

        // 데이터가 없으면 새로운 플레이어
        if ((!playerExists || playerData == null))
        {
            // New Player!
            return await InitializeNewPlayer(context, gameApiClient, authPlayerName);
        }

        // =========================================================
        // 추가: 구글 연동 등으로 이름이 바뀌었을 때를 대비한 자동 업데이트
        // DB에 저장된 이름과 방금 클라이언트가 보내준 이름이 다르다면?
        if (playerData.DisplayName != authPlayerName)
        {
            playerData.DisplayName = authPlayerName; // 구글 이름으로 교체!

            // 바뀐 데이터를 Cloud Save에 덮어씌워 줍니다.
            await SaveData(context, gameApiClient, ServerDefine.k_PlayerDataKey, playerData);

            _logger.LogInformation($"구글 연동 이름 서버 DB 갱신 완료: {authPlayerName}");
        }
        // =========================================================

        // 데이터가 있으면 이코노미 데이터도 가져와서 반환하기
        PlayerEconomyData economyData = await _playerEconomyService.GetPlayerEconomyData(context, gameApiClient);

        // [새로 추가!] 업그레이드 데이터 가져오기
        PlayerUpgradeData upgradeData = await TryGetPlayerUpgradeData(context, gameApiClient);

        return new PlayerDataResponse
        {
            PlayerData = playerData,
            PlayerEconomyData = economyData,
            PlayerUpgradeData = upgradeData, // 응답 객체에 담아서 클라로 전송!
            IsNewPlayer = false
        };
    }
    // 새로운 플레이어 초기화
    private async Task<PlayerDataResponse> InitializeNewPlayer(IExecutionContext context, IGameApiClient gameApiClient, string authPlayerName)
    {
        PlayerData newPlayerData = new PlayerData
        {
            DisplayName = authPlayerName,
            Experience = 0,
            MaxSurviveTime = 0,
            MaxScore = 0,
            MaxClearStage = 0,
            LastDailyFreeGoldClaimDate = string.Empty,
            IsAdsRemoved = false
        };

        PlayerEconomyData newEconomyData;

        // [새로 추가!] 신규 유저용 빈 업그레이드 데이터 생성
        PlayerUpgradeData newUpgradeData = new PlayerUpgradeData();

        try
        {
            // Save new Player Data
            await SaveData(context, gameApiClient, ServerDefine.k_PlayerDataKey, newPlayerData);

            // Initialize new Player inventory
            newEconomyData = await _playerEconomyService.InitializeNewPlayerEconomy(context, gameApiClient);

            //  [새로 추가!] 신규 유저의 업그레이드 데이터를 클라우드에 빈 바구니로 생성
            //await SaveUpgradeData(context, gameApiClient, newUpgradeData);
            await SaveData(context, gameApiClient, ServerDefine.k_PlayerUpgradeKey, newUpgradeData);

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
            PlayerUpgradeData = newUpgradeData, // 응답 객체에 담기
            IsNewPlayer = true
        };
    }

    #region 저장 데이터 가져오기
    // 플레이어 데이터 가져오기
    public async Task<(bool playerExists, PlayerData? playerData)> TryGetPlayerData(IExecutionContext context, IGameApiClient gameApiClient)
    {
        try
        {
            var (success, playerDataJson) = await TryGetData(context, gameApiClient, ServerDefine.k_PlayerDataKey);

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
    // 1. 업그레이드 데이터 불러오기
    public async Task<PlayerUpgradeData> TryGetPlayerUpgradeData(IExecutionContext context, IGameApiClient gameApiClient)
    {
        var (success, json) = await TryGetData(context, gameApiClient, ServerDefine.k_PlayerUpgradeKey);

        //  기존 유저인데 업그레이드 시스템 업데이트 전이라 데이터가 없는 경우!
        if (!success || json == null)
        {
            // 1. 새 빈 데이터 생성
            PlayerUpgradeData newUpgradeData = new PlayerUpgradeData();

            // 2. DB에 확실하게 저장 (마이그레이션 처리)
            await SaveData(context, gameApiClient, ServerDefine.k_PlayerUpgradeKey, newUpgradeData);

            _logger.LogInformation($"기존 유저({context.PlayerId})의 업그레이드 데이터베이스 신규 생성 및 저장 완료.");

            // 3. 클라이언트에게 전달
            return newUpgradeData;
        }

        // 정상적으로 찾았다면 변환해서 반환
        return JsonConvert.DeserializeObject<PlayerUpgradeData>(json) ?? new PlayerUpgradeData();
    }

    #endregion

    #region 클라에서 부르는 데이터 저장 함수

    [CloudCodeFunction("UpdateGameRecord")]
    public async Task<PlayerData> UpdateGameRecord(IExecutionContext context, IGameApiClient gameApiClient, int newScore, int newSurviveTime, int clearStageLevel)
    {
        // 1. 기존 서버에 저장된 내 데이터 불러오기
        var (playerExists, playerData) = await TryGetPlayerData(context, gameApiClient);

        if (!playerExists || playerData == null)
        {
            throw new Exception("플레이어 데이터를 찾을 수 없습니다.");
        }

        bool isUpdated = false;

        // 2. 신기록 달성 체크! (기존 기록보다 높을 때만 갱신)
        if (newScore > playerData.MaxScore)
        {
            playerData.MaxScore = newScore;
            isUpdated = true;
        }

        if (newSurviveTime > playerData.MaxSurviveTime)
        {
            playerData.MaxSurviveTime = newSurviveTime;
            isUpdated = true;
        }

        if (clearStageLevel > playerData.MaxClearStage)
        {
            playerData.MaxClearStage = clearStageLevel;
            isUpdated = true;
        }

        // 3. 기록이 갱신되었다면 서버에 덮어쓰기 (DB 저장)
        if (isUpdated)
        {
            await SaveData(context, gameApiClient, ServerDefine.k_PlayerDataKey, playerData);
            _logger.LogInformation($"Player {context.PlayerId} 신기록 달성! Score: {playerData.MaxScore}");
        }

        // 4. (갱신 여부와 상관없이) 최신 데이터를 클라로 반환
        return playerData;
    }


    [CloudCodeFunction("UpdateUpgradeLevel")]
    public async Task<PlayerUpgradeData> UpdateUpgradeLevel(IExecutionContext context, IGameApiClient gameApiClient, string upgradeType, int newLevel)
    {
        // 1. 기존 데이터 로드
        var upgradeData = await TryGetPlayerUpgradeData(context, gameApiClient);

        // 2. 데이터 수정
        upgradeData.UpgradeLevels[upgradeType] = newLevel;

        // 3. 서버에 저장
        //await SaveUpgradeData(context, gameApiClient, upgradeData);
        await SaveData(context, gameApiClient, ServerDefine.k_PlayerUpgradeKey, upgradeData);

        return upgradeData;
    }
    #endregion
}

