using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudCode.GeneratedBindings.Project;
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

        Managers.PlayerData.PlayerDataUpdated -= RefreshProfile;
        Managers.PlayerData.PlayerDataUpdated += RefreshProfile;

        // 이미 데이터가 있다면 초기 화면을 바로 갱신합니다.
        if (Managers.PlayerData.PlayerDataLocal != null)
        {
            RefreshProfile(Managers.PlayerData.PlayerDataLocal);
        }
    }
    
    // 1. 프로필 정보 갱신 (화면에 뿌려주기)
    public void RefreshProfile(PlayerData playerData)
    {
        // 서버에서 받아온 PlayerData의 DisplayName을 사용합니다.
        string fullName = playerData.DisplayName;

        // 태그(#) 앞부분만 보여주는 로직은 그대로 유지합니다.
        if (fullName.Contains("#"))
        {
            string[] splitName = fullName.Split('#');
            GetTMP((int)Texts.Text_NickName).text = splitName[0];
        }
        else
        {
            GetTMP((int)Texts.Text_NickName).text = fullName;
        }

    }
    public override void Clear()
    {
        base.Clear();
        Managers.PlayerData.PlayerDataUpdated -= RefreshProfile;
    }
}
