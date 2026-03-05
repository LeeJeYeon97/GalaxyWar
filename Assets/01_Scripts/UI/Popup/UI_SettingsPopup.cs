using UnityEngine;
using UnityEngine.UI;

public class UI_SettingsPopup : UI_Popup
{
    enum Buttons
    {
        Btn_ClosePopup,
        
    }
    private void Start()
    {
        Init();
    }
    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));

        
        GetButton((int)Buttons.Btn_ClosePopup).onClick.AddListener(OnClickCancelButton);
    }
    private void OnClickCancelButton()
    {
        ClosePopupUI();
    }
}
