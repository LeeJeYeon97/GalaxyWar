using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class GameScene : BaseScene
{
	protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.GameScene;

        Managers.AD.HideBanner();
        Managers.Game.Init();
    }

    public override void Clear()
    {
        Managers.Game.Clear();
        
    }
}
