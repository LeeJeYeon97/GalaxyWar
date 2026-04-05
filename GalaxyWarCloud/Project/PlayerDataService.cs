using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace Project;

public class PlayerDataService
{
    private static ILogger<PlayerDataService> _logger;

    public PlayerDataService(ILogger<PlayerDataService> logger)
    {
        _logger = logger;
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
            return result.Data.Results.First().Value;
        }
        catch (ApiException ex)
        {
            _logger.LogError("Failed to get data. Error: {Error}", ex.Message);
            throw new Exception($"Failed to get data for playerId {context.PlayerId}. Error: {ex.Message}");
        }
    }


    [CloudCodeFunction("SayHello")]
    public async Task Hello(IExecutionContext context, IGameApiClient gameApiClient, string key, string value)
    {
        await SaveData(context, gameApiClient, key, value);
    }
}


