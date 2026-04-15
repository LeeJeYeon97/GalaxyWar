using TMPro;
using Unity.Services.CloudSave.Models.Data.Player;
using UnityEngine;
using UnityEngine.UI;

public class UI_LoadingPopup : UI_Popup
{
    enum Texts
    {
        Text
    }
    public override void Init()
    {
        base.Init();

        Bind<TMP_Text>(typeof(Texts));

    }
    public void SetText(string text)
    {
        GetTMP((int)Texts.Text).text = text;
    }
}
