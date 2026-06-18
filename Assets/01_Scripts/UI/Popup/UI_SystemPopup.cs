using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SystemPopup : UI_Popup
{
    enum Texts
    {
        MainText
    }
    enum Buttons
    {
        Button_Exit
    }

    //  팝업이 닫힐 때 실행할 함수를 저장해둘 변수
    private Action _onCloseCallback;

    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));

        GetButton((int)Buttons.Button_Exit).onClick.AddListener(OnClickExitButton);
    }
    //  기존 SetText를 확장하거나, 새로운 설정용 함수를 만듭니다.
    public void SetInfo(string text, Action onCloseCallback = null)
    {
        if (!_init) Init(); // 혹시 초기화가 안 되어 있다면 실행

        GetTMP((int)Texts.MainText).text = text;

        // 외부에서 넘겨준 함수를 보관합니다. (안 넘겨주면 null)
        _onCloseCallback = onCloseCallback;
    }

    private void OnClickExitButton()
    {
        // 팝업창을 닫기 직전(또는 직후)에 저장해둔 함수가 있다면 실행합니다.
        // ?.Invoke()는 _onCloseCallback이 null이 아닐 때만 실행하라는 안전 장치입니다.
        _onCloseCallback?.Invoke();

        ClosePopupUI();
    }
}

