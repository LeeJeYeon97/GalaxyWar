using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_MainPanel : UI_Base
{
    enum Buttons
    {

        Button_GameStart,
    }

    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.Button_GameStart).onClick.AddListener(() => Managers.Scene.LoadScene(Define.Scene.GameScene));

    }
    public override void Clear()
    {
        base.Clear();
    }
}
