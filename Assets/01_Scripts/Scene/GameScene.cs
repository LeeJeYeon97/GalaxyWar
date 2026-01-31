using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScene : BaseScene
{
	protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.GameScene;
        Managers.UI.ShowSceneUI<UI_GameScene>();

        Managers.Map.Init();
        Managers.Pool.Init();
        Managers.Game.Init();
	}

    public override void Clear()
    {
        
    }
}
