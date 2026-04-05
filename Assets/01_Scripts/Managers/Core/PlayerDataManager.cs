using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using UnityEngine;
using Unity.Services.CloudSave;

public class PlayerDataManager
{
    public PlayerDataServiceBindings MyModuleBindings;
    public string PlayerName;

    public void Init()
    {
        Managers.Login.OnLoginSuccess += InitializePlayer;

        // 클라우드 코드 바인딩 초기화
        //MyModuleBindings = new MyModuleBindings(CloudCodeService.Instance);
    }

    private async void InitializePlayer()
    {
        try
        {
            // 추가: 로그인이 성공해서 UGS가 완전히 켜진 지금 세팅합니다!
            if (MyModuleBindings == null)
            {
                MyModuleBindings = new PlayerDataServiceBindings(CloudCodeService.Instance);
            }
            string PlayerName = AuthenticationService.Instance.PlayerName;
            string Key = "PLAYER_NAME";
            await MyModuleBindings.SayHello(Key, PlayerName);
            //Debug.Log($"{resultFromCloud}");
        }
        catch(CloudCodeException e)
        {
            Debug.LogException(e);
        }
    }
    public void Clear()
    {
        Managers.Login.OnLoginSuccess -= InitializePlayer;
    }
}
