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

        RefreshText();
    }
    private void RefreshText()
    {
        GetTMP((int)Texts.Text_Score).text = Managers.Level.Score.ToString("N0");

        float time = Managers.Game.gamePlayTime;
        int minutes = Mathf.FloorToInt(time / 60f); // 60으로 나눠서 '분' 계산 (내림)
        int seconds = Mathf.FloorToInt(time % 60f); // 60으로 나눈 나머지로 '초' 계산
        // "00:00" 형식으로 출력 (예: 12:05)
        GetTMP((int)Texts.Text_Time).text = $"{minutes:00}:{seconds:00}";

        GetTMP((int)Texts.Text_KillCount).text = $"{Managers.Game.killCount.ToString("N0")} Kill";

        // 4. 골드 (천 단위 콤마 추가)
        GetTMP((int)Texts.Text_Gold).text = $"{Managers.Game.currentSessionGold.ToString("N0")} G";
    }
    private async void OnClickNextStageButton()
    {
        GetButton((int)Buttons.Btn_NextStage).interactable = false;

        // 1. 로딩 팝업 띄우기
        var popup = Managers.UI.ShowPopupUI<UI_LoadingPopup>();

        // 2. 서버 데이터 저장 완료까지 대기 (클리어 했으므로 true 전달)
        await Managers.PlayerData.SavePlayerData(true);

        // 3. 로딩 창 닫기
        Managers.UI.ClosePopupUI(popup);

        // 4. 다음 스테이지로 이동 (GameScene 다시 로드)
        Managers.Scene.LoadScene(Define.Scene.GameScene);
    }
    private async void OnClickQuitLobbyButton()
    {
        GetButton((int)Buttons.Btn_QuitLobby).interactable = false;

        // 1. 로딩 팝업 띄우기
        var popup = Managers.UI.ShowPopupUI<UI_LoadingPopup>();

        // 2. 서버 데이터 저장 완료까지 대기 (클리어 했으므로 true 전달)
        await Managers.PlayerData.SavePlayerData(true);

        // 3. 로딩 창 닫기
        Managers.UI.ClosePopupUI(popup);

        // 4. 로비로 돌아가기 (광고 후 씬 로드)
        Managers.AD.ShowInterstitialAd(() =>
        {
            Managers.Scene.LoadScene(Define.Scene.LobbyScene);
        });
    }
    private void OnClickRewardButton()
    {
        GetButton((int)Buttons.Btn_RewardDouble).interactable = false;

        // 아이언소스 보상형 광고 호출
        Managers.AD.ShowRewardedAd(placement_GameOver, async (success) =>
        {
            if (success)
            {
                Debug.Log("보상 두 배 광고 시청 완료!");

                // 1. 보상 두배 적용
                Managers.Game.currentSessionGold *= 2;

                // 2. 로딩 팝업 띄우기
                var popup = Managers.UI.ShowPopupUI<UI_LoadingPopup>();

                // 3. 두 배가 된 골드와 기록을 서버에 저장 (클리어 했으므로 true 전달)
                await Managers.PlayerData.SavePlayerData(true);

                // 4. 로딩 창 닫기
                Managers.UI.ClosePopupUI(popup);

                // 5. 로비로 이동
                Managers.Scene.LoadScene(Define.Scene.LobbyScene);
            }
            else
            {
                Debug.Log("보상 두 배 광고 시청 실패 또는 취소.");

                // 광고를 중간에 껐을 경우 다시 누를 수 있도록 버튼 활성화 (선택 사항)
                GetButton((int)Buttons.Btn_RewardDouble).interactable = true;
            }
        });
    }

    //private async Task SaveSessionData()
    //{
    //    int sessionGold = Managers.Game.currentSessionGold;

    //    var popup = Managers.UI.ShowPopupUI<UI_LoadingPopup>();
    //    // 얻은 골드가 0보다 클 때만 서버에 요청을 보냅니다.
    //    if (sessionGold > 0)
    //    {
    //        Debug.Log($"서버에 {sessionGold} 골드 저장을 요청합니다...");

    //        // 저번에 만든 PlayerEconomyManager의 기능을 활용합니다.
    //        // (함수명은 프로젝트의 Economy 관리자 구현에 맞게 맞추시면 됩니다)
    //        bool success = await Managers.PlayerEconomy.AddGoldAsync(sessionGold);

    //        if (success)
    //            Debug.Log("골드 저장 성공!");
    //        else
    //            Debug.LogWarning("골드 저장 실패. 네트워크 상태를 확인하세요.");
    //    }

    //    // 2. 플레이어 최고 기록(점수, 생존 시간) 저장
    //    Debug.Log("서버에 플레이어 기록 저장을 요청합니다...");

    //    // PlayerDataManager에 만들어둔 저장 함수를 호출하고 끝날 때까지 기다립니다.
    //    await Managers.PlayerData.SavePlayerData(true);

    //    Managers.UI.ClosePopupUI(popup);
    //}
}
