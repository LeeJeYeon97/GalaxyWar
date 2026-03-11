using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_GameOverPopup : UI_Popup
{
    enum Buttons
    {  
        Btn_Restart,
        Btn_RewardAD,
        Btn_QuitLobby
    }
    enum Texts
    {
        RewardCountText,
    }
    private void Start()
    {
        Init();
    }
    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));

        GetButton((int)Buttons.Btn_Restart).onClick.AddListener(OnClickRestartButton);
        GetButton((int)Buttons.Btn_QuitLobby).onClick.AddListener(OnClickQuitLobbyButton);


        Button rewardButton = GetButton((int)Buttons.Btn_RewardAD);
        rewardButton.onClick.AddListener(OnClickRewardADButton);

        if(Managers.Game.reviveCount <= 0)
        {
            rewardButton.gameObject.SetActive(false);
        }

        GetTMP((int)Texts.RewardCountText).text = $"남은 횟수 : {Managers.Game.reviveCount}";

    }
    private void OnClickRestartButton()
    {
        // 현재 씬(GameScene)을 다시 로드! (가장 깔끔한 초기화)
        Managers.Scene.LoadScene(Define.Scene.GameScene);
    }
    private void OnClickQuitLobbyButton()
    {
        // 로비로 돌아가기
        // 현재 씬(GameScene)을 다시 로드! (가장 깔끔한 초기화)
        Managers.AD.ShowInterstitialAd();

        Managers.Scene.LoadScene(Define.Scene.LobbyScene);
    }
    private void OnClickRewardADButton()
    {

        if (Time.timeScale == 0.0f)
        {
            Time.timeScale = 1f;
        }
        if (Managers.Game.reviveCount <= 0)
        {
            return;
        }
        Managers.AD.ShowRewardedAd();

        ClosePopupUI();
    }
}
