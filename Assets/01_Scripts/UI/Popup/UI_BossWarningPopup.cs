using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;


public class UI_BossWarningPopup : UI_Popup
{//  에디터의 자식 오브젝트 이름과 정확히 일치해야 합니다!
    enum Images
    {
        Image_WarningBackground, // 빨간색 전체 테두리나 배경
        Image_WarningIcon,
        Image_WariningText

    }

    enum Texts
    {
        Text_WarningMessage     // "WARNING!" 글자
    }

    public override void Init()
    {
        base.Init();

        // 1. UI 컴포넌트 바인딩
        Bind<Image>(typeof(Images));
        Bind<TMP_Text>(typeof(Texts));

        // 2. 바인딩된 컴포넌트 가져오기
        Image warningBg = GetImage((int)Images.Image_WarningBackground);
        Image warningIcon = GetImage((int)Images.Image_WarningIcon);
        Image warningTextTitle = GetImage((int)Images.Image_WariningText);
        TMP_Text warningText = GetTMP((int)Texts.Text_WarningMessage);



        // =========================================================
        //  3. 깜빡임 연출 (DOTween Yoyo)
        // =========================================================

        // 배경 이미지: 알파(투명도)값을 0.2f로 낮추는 애니메이션을 0.5초 동안 실행
        // SetLoops(-1, LoopType.Yoyo): 무한(-1)으로 원래 상태와 왕복(Yoyo)해라!
        if (warningBg != null)
        {
            warningBg.DOFade(0.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetLink(warningBg.gameObject);
        }
        if (warningIcon != null)
        {
            warningIcon.DOFade(0.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetLink(warningIcon.gameObject);
        }
        if (warningTextTitle != null)
        {
            warningTextTitle.DOFade(0.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetLink(warningTextTitle.gameObject);
        }

        // 텍스트: 알파값을 0f(완전 투명)으로 0.3초 동안 빠르고 강렬하게 깜빡임
        if (warningText != null)
        {
            warningText.DOFade(0f, 0.3f).SetLoops(-1, LoopType.Yoyo).SetLink(warningText.gameObject);
        }

        // =========================================================
        //  4. 자동 닫기 (3초 뒤에 알아서 꺼지도록 세팅)
        // =========================================================
        // DOVirtual.DelayedCall을 쓰면 코루틴 없이도 특정 시간 뒤에 함수를 실행할 수 있습니다.
        DOVirtual.DelayedCall(6f, () =>
        {
            // Managers.UI.ClosePopupUI(this); 
            // (대표님의 UI 매니저 닫기 함수 이름에 맞게 수정해주세요!)
            Managers.UI.ClosePopupUI(this);
        }).SetLink(gameObject);
    }
}
