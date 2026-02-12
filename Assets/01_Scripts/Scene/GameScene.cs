using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScene : BaseScene
{
	protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.GameScene;
        
        
        Managers.Map.Init();
        Managers.Game.Init();
        Managers.UI.Init();
	}

    public override void Clear()
    {
        
    }
}
