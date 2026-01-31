using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_LobbyScene : UI_Scene
{
    enum Buttons
    {
        StartButton,
        ExitButton,
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
        startButton.onClick.AddListener(() => Managers.Scene.LoadScene(Define.Scene.GameScene));

        GetButton((int)Buttons.ExitButton);
        GetButton((int)Buttons.SettingButton);
    }
}
