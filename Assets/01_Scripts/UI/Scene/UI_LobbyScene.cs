using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_LobbyScene : UI_Scene
{
    enum Buttons
    {
        StartButton,
        ShopButton,
        SettingButton,
    }

    private void Start()
    {
        Init();
    }
    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));

        ButtonSetting();
    }
    public void ButtonSetting()
    {
        Button startButton = GetButton((int)Buttons.StartButton);
        startButton.onClick.AddListener(OnClickStartButton);

        GetButton((int)Buttons.SettingButton).onClick.AddListener(OnClickSettingButton);

        GetButton((int)Buttons.ShopButton).onClick.AddListener(OnClickShopButton);
    }
    private void OnClickShopButton()
    {
        Debug.Log("TODO : 상점 페이지 만들기");
    }
    private void OnClickSettingButton()
    {

        Debug.Log("TODO : 세팅 페이지 만들기");
    }
    private void OnClickStartButton()
    {
        Managers.Sound.Play(SoundID.Sfx_UIButtonClick);
        Managers.Scene.LoadScene(Define.Scene.GameScene);
    }
}
