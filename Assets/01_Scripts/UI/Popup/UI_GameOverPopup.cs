using TMPro;
using UnityEngine;
using UnityEngine.Localization;
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
    [SerializeField]
    private LocalizedString _localizedRewardCountText;


    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));

        GetButton((int)Buttons.Btn_Restart).onClick.AddListener(OnClickRestartButton);
        GetButton((int)Buttons.Btn_QuitLobby).onClick.AddListener(OnClickQuitLobbyButton);


        Button rewardButton = GetButton((int)Buttons.Btn_RewardAD);
        rewardButton.onClick.AddListener(OnClickRewardADButton);

        if(Managers.Game.reviveCount <= 0)
        {
            rewardButton.gameObject.SetActive(false);
        }

        RefreshRewardCountText();
    }
    //유니티 눈치 안 보고 내가 원할 때 직접 번역본을 가져오는 마법의 함수!
    private void RefreshRewardCountText()
    {
        // 1. {0} 에 들어갈 숫자를 확실하게 상자에 넣어줍니다.
        _localizedRewardCountText.Arguments = new object[] { Managers.Game.reviveCount };

        // 2. "지금 당장 저 숫자 넣어서 완벽하게 번역된 문장 내놔!" 라고 요청합니다.
        var op = _localizedRewardCountText.GetLocalizedStringAsync();

        // 3. 번역이 완료되면 바로 UI에 꽂아 넣습니다.
        op.Completed += (handle) =>
        {
            GetTMP((int)Texts.RewardCountText).text = handle.Result;
        };
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
