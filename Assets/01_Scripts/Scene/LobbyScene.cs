using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Define;

public class LobbyScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.LobbyScene;

        //Managers.AD.ShowBanner();
        Time.timeScale = 1f;
        // 2. BGM 재생 (확장자인 .mp3 등은 빼고 파일 이름만 적습니다)
        Managers.Sound.Play(SoundID.Bgm_Lobby,Sound.Bgm);
        Managers.UI.ShowSceneUI<UI_LobbyScene>();
    }
    public override void Clear()
    {

    }
}
