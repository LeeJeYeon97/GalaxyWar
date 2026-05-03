using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using UnityEngine;
using Unity.Services.CloudSave;
using Unity.Services.CloudCode.GeneratedBindings;
using Newtonsoft.Json;
using Unity.Services.CloudCode.GeneratedBindings.Project;
using System.Threading.Tasks;

public class PlayerDataManager
{
    public PlayerDataServiceBindings playerDataServiceBindings;

    public event Action<PlayerData> PlayerDataUpdated;

    public PlayerData PlayerDataLocal;

    public void Init()
    {
        Managers.Login.OnLoginSuccess -= InitializePlayer;
        Managers.Login.OnLoginSuccess += InitializePlayer;

        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
        Managers.Initialize.OnUnityServiceInit += SetupBindings;

    }
    // 2. 바인딩 세팅용 함수를 따로 만듭니다.
    private void SetupBindings()
    {
        if (playerDataServiceBindings == null)
        {
            playerDataServiceBindings = new PlayerDataServiceBindings(CloudCodeService.Instance);
        }
    }
    private async void InitializePlayer()
    {
        try
        {
            // 추가: 로그인이 성공해서 UGS가 완전히 켜진 지금 세팅합니다!
            
            // 추가: Auth 시스템이 가지고 있는 내 닉네임(태그 포함)을 가져옵니다.
            string myName = AuthenticationService.Instance.PlayerName;

            var playerDataResponse = await playerDataServiceBindings.HandlePlayerSignIn(myName);

            PlayerDataLocal = playerDataResponse.PlayerData;
            PlayerDataUpdated?.Invoke(PlayerDataLocal);

            Managers.PlayerEconomy.HandleEconomyUpdate(playerDataResponse.PlayerEconomyData);
            Managers.PlayerEconomy.CheckAdRemovalStatus();

            LogResponse(playerDataResponse);


        }
        catch(CloudCodeException e)
        {
            Debug.LogException(e);
        }
    }
    public async Task SavePlayerData()
    {
        // 2. 최고 기록(점수, 생존 시간) 저장 로직 추가!
        try
        {
            int finalScore = Managers.Level.Score; 
            int finalTime = Mathf.FloorToInt(Managers.Game.gamePlayTime);

            // Cloud Code 바인딩을 통해 서버의 UpdateGameRecord 호출
            var updatedData = await playerDataServiceBindings.UpdateGameRecord(finalScore, finalTime);

            Debug.Log($"기록 저장 완료! 현재 최고 점수: {updatedData.MaxScore}");

            // 필요하다면 Managers.Data 쪽의 클라이언트 메모리 데이터도 업데이트해 줍니다.
            PlayerDataLocal = updatedData;
            PlayerDataUpdated?.Invoke(PlayerDataLocal);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"기록 저장 실패: {ex.Message}");
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
}
