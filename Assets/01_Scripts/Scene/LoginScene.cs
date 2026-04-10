using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using static Define;

public class LoginScene : BaseScene
{
    protected override async void Init()
    {
        base.Init();

        SceneType = Define.Scene.LoginScene;

        // 2. BGM 재생 (확장자인 .mp3 등은 빼고 파일 이름만 적습니다)
        Managers.Sound.Play(SoundID.Bgm_Lobby, Sound.Bgm);

        Managers.UI.ShowSceneUI<UI_LoginScene>();

        // 서버 및 구글 플레이 초기화
        await Managers.Initialize.Init();
    }
    public override void Clear()
    {
    }
}
