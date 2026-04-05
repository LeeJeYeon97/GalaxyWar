using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_PausePopup : UI_Popup
{
    enum Buttons
    {
        Btn_Resume,
        Btn_Restart,
        Btn_Settings,
        Btn_QuitGame
    }
    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.Btn_Resume).onClick.AddListener(OnClickResumeButton);
        GetButton((int)Buttons.Btn_Restart).onClick.AddListener(OnClickRestartButton);
        GetButton((int)Buttons.Btn_Settings).onClick.AddListener(OnClickSettingButton);
        GetButton((int)Buttons.Btn_QuitGame).onClick.AddListener(OnClickQuitGameButton);

    }
    private void OnClickResumeButton()
    {
        ClosePopupUI();
        Managers.Game.ChangeGameState(GameState.Resume);
    }
    private void OnClickRestartButton()
    {
        // 현재 씬(GameScene)을 다시 로드! (가장 깔끔한 초기화)
        Managers.Scene.LoadScene(Define.Scene.GameScene);
    }
    private void OnClickSettingButton()
    {
        Managers.UI.ShowPopupUI<UI_SettingsPopup>();
    }
    private void OnClickQuitGameButton()
    {
        Managers.UI.ShowPopupUI<UI_QuitGamePopup>();
    }
}
