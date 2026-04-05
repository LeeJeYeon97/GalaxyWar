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

    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));

        GetButton((int)Buttons.Button_Exit).onClick.AddListener(OnClickExitButton);
    }
    public void SetText(string text)
    {
        GetTMP((int)Texts.MainText).text = text;
    }

    private void OnClickExitButton()
    {
        ClosePopupUI();
    }
}

