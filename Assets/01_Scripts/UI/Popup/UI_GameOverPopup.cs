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
    private void Start()
    {
        Init();
    }
    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.Btn_Restart).onClick.AddListener(OnClickRestartButton);
        GetButton((int)Buttons.Btn_RewardAD).onClick.AddListener(OnClickRewardADButton);

        GetButton((int)Buttons.Btn_QuitLobby).onClick.AddListener(OnClickQuitLobbyButton);

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
        Managers.Scene.LoadScene(Define.Scene.LobbyScene);
    }
    private void OnClickRewardADButton()
    {
        Time.timeScale = 1f;
        // TODO
        Managers.Game.RevivePlayer();

        ClosePopupUI();
    }
}
