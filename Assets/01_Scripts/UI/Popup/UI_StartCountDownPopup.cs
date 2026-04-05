using DG.Tweening;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;
using static UnityEngine.Rendering.GPUSort;

public class UI_StartCountDownPopup : UI_Popup
{
    enum Texts
    {
        text
    }

    private TMP_Text _text;
    

    
    public override void Init()
    {
        base.Init();

        Bind<TMP_Text>(typeof(Texts));

        _text = GetTMP((int)Texts.text);
        _text.text = "";
        _text.GetComponent<RectTransform>().anchoredPosition = new Vector2(-1200f, 0f);

        // 카운트다운 시작! (경고를 없애기 위해 _ = 사용)
        _ = StartCountdownAsync();
    }

    private async Task StartCountdownAsync()
    {
        // 텍스트를 움직이려면 RectTransform이 필요
        RectTransform textRect = _text.GetComponent<RectTransform>();

        int startCount = Managers.Data.GameData.GameStartTime;
        // 1초마다 다음 이미지를 켭니다 (총 5번 반복)
        for (int i = startCount; i > 0; i--)
        {
            // 1. 텍스트를 현재 숫자(5, 4, 3, 2, 1)로 변경
            _text.text = i.ToString();

            // 2. 텍스트 시작 위치를 왼쪽 화면 밖(-1200)으로 초기화
            textRect.anchoredPosition = new Vector2(-1200f, 0f);

            // 3. DOTween 시퀀스(타임라인) 생성
            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true); // 시간 정지 상태 대응

            // [콤보 1] 등장: 왼쪽에서 정중앙(0)으로 0.2초 만에 아주 빠르게 날아옵니다! (EaseOut)
            seq.Append(textRect.DOAnchorPosX(0f, 0.2f).SetEase(Ease.OutCubic));

            // [콤보 2] 슬로우 모션: 정중앙에서 살짝 오른쪽(+100)까지 0.6초 동안 천천히 이동합니다! (Linear)
            // 플레이어가 숫자를 읽을 수 있는 '체공 시간'을 줍니다.
            seq.Append(textRect.DOAnchorPosX(50f, 0.6f).SetEase(Ease.Linear));

            // [콤보 3] 퇴장: 오른쪽 화면 밖(+1200)으로 0.2초 만에 다시 빠르게 날아갑니다! (EaseIn)
            seq.Append(textRect.DOAnchorPosX(1200f, 0.2f).SetEase(Ease.InCubic));

            // 핵심: Task.Delay(1000) 대신, 이 애니메이션(총 1초)이 끝날 때까지 딱 맞춰서 기다립니다!
            await seq.AsyncWaitForCompletion();

            // 1초(1000밀리초) 대기합니다
            //await Task.Delay(1000);

        }

        _text.text = "GO!";
        textRect.anchoredPosition = new Vector2(-1200f, 0f);

        Sequence goSeq = DOTween.Sequence();
        goSeq.SetUpdate(true);
        goSeq.Append(textRect.DOAnchorPosX(0f, 0.2f).SetEase(Ease.OutBack)); // GO는 통통 튀게!
        goSeq.AppendInterval(0.5f); // 0.5초 동안 중앙에 정지해서 보여줌
        goSeq.Append(textRect.DOAnchorPosX(1200f, 0.2f).SetEase(Ease.InCubic));
        

        await goSeq.AsyncWaitForCompletion(); // GO 애니메이션 끝날 때까지 대기

        Managers.Sound.Play(SoundID.Bgm_Game,Sound.Bgm);
        Managers.Game.ChangeGameState(GameState.Playing);

        Managers.UI.ClosePopupUI(this);
    }

}
