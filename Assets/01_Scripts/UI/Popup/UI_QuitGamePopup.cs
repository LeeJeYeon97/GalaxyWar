using UnityEngine;
using UnityEngine.UI;

public class UI_QuitGamePopup : UI_Popup
{
    enum Buttons
    {
        Btn_ClosePopup,
        Btn_Yes,
        Btn_Cancel
    }
    private void Start()
    {
        Init();
    }
    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.Btn_Yes).onClick.AddListener(OnClickYesButton);
        GetButton((int)Buttons.Btn_Cancel).onClick.AddListener(OnClickCancelButton);
        GetButton((int)Buttons.Btn_ClosePopup).onClick.AddListener(OnClickCancelButton);
    }
    private void OnClickYesButton()
    {
        // 현재 씬(GameScene)을 다시 로드! (가장 깔끔한 초기화)
        Managers.Scene.LoadScene(Define.Scene.LobbyScene);
    }
    private void OnClickCancelButton() 
    {
        ClosePopupUI();
    }
}
