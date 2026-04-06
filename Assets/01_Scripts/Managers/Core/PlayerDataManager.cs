using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using UnityEngine;
using Unity.Services.CloudSave;
using Unity.Services.CloudCode.GeneratedBindings;
using Newtonsoft.Json;
using Unity.Services.CloudCode.GeneratedBindings.Project;

public class PlayerDataManager
{
    public PlayerDataServiceBindings playerDataServiceBindings;
    public PlayerEconomyServiceBindings playerEconomyServiceBindings;

    public event Action<PlayerData> PlayerDataUpdated;

    public PlayerData PlayerDataLocal;
    public void Init()
    {
        Managers.Login.OnLoginSuccess += InitializePlayer;

        // 클라우드 코드 바인딩 초기화
        //MyModuleBindings = new MyModuleBindings(CloudCodeService.Instance);
    }

    private async void InitializePlayer()
    {
        try
        {
            // 추가: 로그인이 성공해서 UGS가 완전히 켜진 지금 세팅합니다!
            if (playerDataServiceBindings == null)
            {
                playerDataServiceBindings = new PlayerDataServiceBindings(CloudCodeService.Instance);
            }
            if(playerEconomyServiceBindings == null)
            {
                playerEconomyServiceBindings = new PlayerEconomyServiceBindings(CloudCodeService.Instance);
            }

            var playerDataResponse = await playerDataServiceBindings.HandlePlayerSignIn();
            PlayerDataLocal = playerDataResponse.PlayerData;
            PlayerDataUpdated?.Invoke(PlayerDataLocal);
            LogResponse(playerDataResponse);

            //await playerEconomyServiceBindings.AddHealthPotion();

        }
        catch(CloudCodeException e)
        {
            Debug.LogException(e);
        }
    }
    private void LogResponse(PlayerDataResponse response)
    {
        string economyJson = JsonConvert.SerializeObject(response.PlayerEconomyData, Formatting.Indented);
        Debug.Log(
            $"====== Player Sign-In Response =====\n" +
            $"Name : {response.PlayerData.DisplayName}\n" +
            $"New Player : {response.IsNewPlayer} \n" +
            $"XP : {response.PlayerData.Experience} \n" +
            $"Economy : {economyJson}\n" +
            $"==============================="
            );
    }
    public void Clear()
    {
        Managers.Login.OnLoginSuccess -= InitializePlayer;
    }
}
