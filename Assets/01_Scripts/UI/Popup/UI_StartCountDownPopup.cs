using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Define;
using static UnityEngine.Rendering.GPUSort;

public class UI_StartCountDownPopup : UI_Popup
{
    enum Images
    {
        SciFi_5,
        SciFi_4,
        SciFi_3,
        SciFi_2,
        SciFi_1,
        Btn_Go
    }

    private void Start()
    {
        Init();
    }

    private Image[] countdownImages;

    public override void Init()
    {
        base.Init();

        Bind<Image>(typeof(Images));

        // 이미지를 담을 배열 생성
        countdownImages = new Image[6];
        for (int i = 0; i <= 5; i++)
        {
            // enum 값을 정수로 캐스팅한 뒤 i를 더해서 다음 카드를 가져옴
            countdownImages[i] = GetImage((int)Images.SciFi_5 + i);
            countdownImages[i].gameObject.SetActive(false);
        }

        // 카운트다운 시작! (경고를 없애기 위해 _ = 사용)
        _ = StartCountdownAsync();
    }

    private async Task StartCountdownAsync()
    {
        // 1초마다 다음 이미지를 켭니다 (총 5번 반복)
        for (int i = 0; i <= 5; i++)
        {
            // 현재 순서의 이미지를 켭니다
            countdownImages[i].gameObject.SetActive(true);

            // 1초(1000밀리초) 대기합니다
            await Task.Delay(1000);

            // 다음 숫자를 켜기 전에 지금 켜져있는 이미지를 끕니다
            // (만약 이전 숫자가 계속 겹쳐서 보여야 한다면 이 줄을 지우세요)
            countdownImages[i].gameObject.SetActive(false);
        }

        // (선택) GO 버튼을 1초간 보여준 뒤 팝업 자체를 닫고 싶다면:
        //await Task.Delay(1000);
        Managers.Sound.Play(SoundID.Bgm_Game,Sound.Bgm);
        Managers.Game.ChangeGameState(GameState.Playing);

        Managers.UI.ClosePopupUI(this);
    }

}
