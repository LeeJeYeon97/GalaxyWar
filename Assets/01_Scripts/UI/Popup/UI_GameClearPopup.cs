using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using static Define;

public class UI_GameClearPopup : UI_Popup
{
    enum Buttons
    {
        Btn_RewardDouble,
        Btn_NextStage,
        Btn_QuitLobby
    }
    enum Texts
    {
        Text_Score,
        Text_Time,
        Text_KillCount,
        Text_Gold
    }


    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));


        GetButton((int)Buttons.Btn_RewardDouble).onClick.AddListener(OnClickRewardButton);
        GetButton((int)Buttons.Btn_NextStage).onClick.AddListener(OnClickNextStageButton);
        GetButton((int)Buttons.Btn_QuitLobby).onClick.AddListener(OnClickQuitLobbyButton);

    }
    
    private async void OnClickNextStageButton()
    {

        GetButton((int)Buttons.Btn_NextStage).interactable = false;
        //// 1. 중복 클릭 방지를 위해 버튼 비활성화
        //GetButton((int)Buttons.Btn_Restart).interactable = false;
        //
        //// 2. 서버 데이터 저장 완료까지 대기
        //await SaveSessionData();
        //
        //// 로비로 돌아가기
        //// 현재 씬(GameScene)을 다시 로드! (가장 깔끔한 초기화)
        //Managers.AD.ShowInterstitialAd(() =>
        //{
        //    // 이 중괄호 안의 코드는 유저가 광고를 [X] 버튼으로 닫거나, 
        //    // 쿨타임 등으로 광고가 스킵되었을 때만 실행됩니다!
        //    Managers.Scene.LoadScene(Define.Scene.GameScene);
        //});

        await SaveSessionData();

        // 다음 스테이지로 가기
        // 현재 씬(GameScene)을 다시 로드! (가장 깔끔한 초기화)
        Managers.AD.ShowInterstitialAd(() =>
        {
            // 이 중괄호 안의 코드는 유저가 광고를 [X] 버튼으로 닫거나, 
            // 쿨타임 등으로 광고가 스킵되었을 때만 실행됩니다!
            Managers.Scene.LoadScene(Define.Scene.LobbyScene);
        });
    }
    private async void OnClickQuitLobbyButton()
    {
        // 1. 중복 클릭 방지
        GetButton((int)Buttons.Btn_QuitLobby).interactable = false;

        // 2. 서버 데이터 저장 완료까지 대기
        await SaveSessionData();

        // 로비로 돌아가기
        // 현재 씬(GameScene)을 다시 로드! (가장 깔끔한 초기화)
        Managers.AD.ShowInterstitialAd(() =>
        {
            // 이 중괄호 안의 코드는 유저가 광고를 [X] 버튼으로 닫거나, 
            // 쿨타임 등으로 광고가 스킵되었을 때만 실행됩니다!
            Managers.Scene.LoadScene(Define.Scene.LobbyScene);
        });
    }
    private void OnClickRewardButton()
    {
        // 클릭 중복 방지 (광고 로딩 중 버튼 끄기)
        GetButton((int)Buttons.Btn_RewardDouble).interactable = false;

        // 아이언소스 보상형 광고 호출 (플레이스먼트 이름은 대시보드에 맞게 수정하세요)
        Managers.AD.ShowRewardedAd(placement_GameOver, (success) =>
        {
            if (success)
            {
                Debug.Log("보상 두 배 광고 시청 완료!");

                // 보상 두배 제공

                Managers.UI.ClosePopupUI();
            }
            else
            {
                Debug.Log("부활 광고 시청 실패 또는 취소.");

            }
        });
    }

    private async Task SaveSessionData()
    {
        int sessionGold = Managers.Game.currentSessionGold;

        // 얻은 골드가 0보다 클 때만 서버에 요청을 보냅니다.
        if (sessionGold > 0)
        {
            Debug.Log($"서버에 {sessionGold} 골드 저장을 요청합니다...");

            // 저번에 만든 PlayerEconomyManager의 기능을 활용합니다.
            // (함수명은 프로젝트의 Economy 관리자 구현에 맞게 맞추시면 됩니다)
            bool success = await Managers.PlayerEconomy.AddGoldAsync(sessionGold);

            if (success)
                Debug.Log("골드 저장 성공!");
            else
                Debug.LogWarning("골드 저장 실패. 네트워크 상태를 확인하세요.");
        }

        // 2. 플레이어 최고 기록(점수, 생존 시간) 저장
        Debug.Log("서버에 플레이어 기록 저장을 요청합니다...");

        // PlayerDataManager에 만들어둔 저장 함수를 호출하고 끝날 때까지 기다립니다.
        await Managers.PlayerData.SavePlayerData(false);
    }
}
