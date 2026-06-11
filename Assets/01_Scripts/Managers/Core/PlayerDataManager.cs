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
            // =========================================================
            // [핵심 추가] 서버에서 받아온 업그레이드 데이터를 UpgradeManager에 세팅!
            // =========================================================
            if (playerDataResponse.PlayerUpgradeData != null)
            {
                Managers.Upgrade.InitializeServerData(playerDataResponse.PlayerUpgradeData.UpgradeLevels);
            }

            LogResponse(playerDataResponse);

            // =======================================================
            //  2. 교통정리 끝! 데이터가 무조건 존재함이 보장되는 이 시점에 IAP를 켭니다.
            // =======================================================
            Debug.Log("[시스템] 서버 데이터 로딩 완료! 이제 상점 카탈로그와 IAP를 시작합니다.");
            Managers.PlayerEconomy.SyncEconomyConfig();

        }
        catch(CloudCodeException e)
        {
            Debug.LogException(e);
        }
    }
    public void UpdatedPlayerData(PlayerData data)
    {
        PlayerDataLocal = data;
        PlayerDataUpdated?.Invoke(PlayerDataLocal);
    }
    
    public async Task SavePlayerData(bool isCleared)
    {
        try
        {
            // =========================================================
            // 1. [통합된 부분] 이번 세션에서 획득한 골드 저장
            // =========================================================
            int sessionGold = Managers.Game.currentSessionGold;
            if (sessionGold > 0)
            {
                Debug.Log($"서버에 {sessionGold} 골드 저장을 요청합니다...");
                bool success = await Managers.PlayerEconomy.AddGoldAsync(sessionGold);

                if (success)
                    Debug.Log("골드 저장 성공!");
                else
                    Debug.LogWarning("골드 저장 실패. 네트워크 상태를 확인하세요.");
            }

            // =========================================================
            // 2. 최고 기록(점수, 생존 시간, 스테이지) 저장
            // =========================================================
            int finalScore = Managers.Level.Score;
            int finalTime = Mathf.FloorToInt(Managers.Game.gamePlayTime);

            int clearStage = isCleared ? Managers.Stage.currentStageLevel : PlayerDataLocal.MaxClearStage;

            var updatedData = await playerDataServiceBindings.UpdateGameRecord(finalScore, finalTime, clearStage);

            Debug.Log($"기록 저장 완료! 현재 최고 점수: {updatedData.MaxScore}");

            PlayerDataLocal = updatedData;
            PlayerDataUpdated?.Invoke(PlayerDataLocal);

            // =========================================================
            //  3. [추가된 부분] 리더보드에 최고 클리어 스테이지 갱신!
            // =========================================================
            // 서버에서 방금 업데이트된 가장 정확한 최고 스테이지(MaxClearStage)를 리더보드로 보냅니다.
            // 리더보드 설정이 'Keep best(최고 기록 유지)'로 되어 있다면, 
            // 매판마다 기록을 던져도 알아서 최고 스테이지만 저장해 줍니다!
            if (Managers.Leaderboard != null)
            {
                Managers.Leaderboard.SubmitScore(updatedData.MaxClearStage);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"기록 저장 실패: {ex.Message}");
        }
    }
    // =========================================================
    // [새로 추가] 특정 업그레이드 항목만 서버에 저장하는 전용 함수
    // =========================================================
    public async Task<bool> SaveUpgradeDataAsync(Define.UpgradeType type, int newLevel)
    {
        try
        {
            // Enum을 서버가 알아들을 수 있게 string으로 변환
            string upgradeTypeStr = type.ToString();

            // Cloud Code의 "UpdateUpgradeLevel" 함수 호출!
            var updatedUpgradeData = await playerDataServiceBindings.UpdateUpgradeLevel(upgradeTypeStr, newLevel);

            Debug.Log($"[PlayerDataManager] 업그레이드 데이터 서버 저장 완료: {upgradeTypeStr} -> Lv.{newLevel}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayerDataManager] 업그레이드 데이터 서버 저장 실패: {ex.Message}");
            return false;
        }
    }

    private void LogResponse(PlayerDataResponse response)
    {
        string economyJson = JsonConvert.SerializeObject(response.PlayerEconomyData, Formatting.Indented);
        string upgradeJson = JsonConvert.SerializeObject(response.PlayerUpgradeData, Formatting.Indented); //  추가

        Debug.Log(
            $"====== Player Sign-In Response =====\n" +
            $"Name : {response.PlayerData.DisplayName}\n" +
            $"New Player : {response.IsNewPlayer} \n" +
            $"XP : {response.PlayerData.Experience} \n" +
            $"Economy : {economyJson}\n" +
            $"Upgrade : {upgradeJson}\n" + //  추가
            $"==============================="
            );
    }
}
