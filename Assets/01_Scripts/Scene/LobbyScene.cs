using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.LobbyScene;

        Managers.UI.ShowSceneUI<UI_LobbyScene>();
    }
    public override void Clear()
    {

    }
}
