using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class UI_Profile : UI_Base
{
    enum Images
    {
        Image_Avatar
    }
    enum Texts
    {
        Text_NickName,
    }
    public override void Init()
    {
        base.Init();

        Bind<Image>(typeof(Images));
        Bind<TMP_Text>(typeof(Texts));

        Managers.Login.OnLoginSuccess -= RefreshProfile;
        Managers.Login.OnLoginSuccess += RefreshProfile;

        RefreshProfile();
    }
    
    // 1. 프로필 정보 갱신 (화면에 뿌려주기)
    public void RefreshProfile()
    {
        string fullName = AuthenticationService.Instance.PlayerName;

        if (string.IsNullOrEmpty(fullName))
        {
            GetTMP((int)Texts.Text_NickName).text = "새로운 모험가";
        }
        else
        {
            // 문자열에 '#'이 포함되어 있다면 그 앞부분만 보여주기
            // 예: "신병6638#2641" -> "신병6638"
            string[] splitName = fullName.Split('#');
            GetTMP((int)Texts.Text_NickName).text = splitName[0];
        }
    }
    public override void Clear()
    {
        base.Clear();

        Managers.Login.OnLoginSuccess -= RefreshProfile;
    }
}
