using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using Unity.Services.CloudCode.GeneratedBindings.Project;
using Unity.Services.Economy;
using UnityEngine;

public class PlayerEconomyManager
{
    // [1] 서버 통신용 바인딩 객체
    // Cloud Code(서버)에 있는 함수를 원격으로 호출할 수 있게 해주는 연결선
    public PlayerEconomyServiceBindings playerEconomyServiceBindings { get; private set; }

    // [2] 로컬 캐시 (내 지갑)
    // 서버에서 받아온 내 재화(골드 등)와 인벤토리 데이터를 저장해 두는 '복사본'입니다.
    // get은 누구나 할 수 있지만, set은 이 클래스 내부에서만 가능하도록(private set) 막아두었습니다. (보안)
    // new Dictionary... 로 초기화해 둔 이유는 처음에 null 에러가 터지는 것을 막기 위한 센스입니다!
    public PlayerEconomyData EconomyDataLocal { get; private set; } = new PlayerEconomyData()
    {
        Currencies = new Dictionary<string, int>(),
        ItemInventory = new Dictionary<string, int>()
    };

    // [3] 편의성 프로퍼티 (Getter)
    // 게임 내에서 가장 자주 쓰이는 '골드'를 매번 GetCurrencyAmount("GOLD")로 부르기 귀찮으므로,
    // Managers.PlayerEconomy.Gold 로 짧고 쉽게 부르기 위해 만든 지름길입니다.

    public int Gold
    {
        get => GetCurrencyAmount(Define.k_GoldCurrencyKey);
    }

    // [4] 이벤트 방송국 (Action)
    // PlayerEconomyUpdated: 내 재화/인벤토리가 바뀌었을 때 UI들에게 "화면 새로고침 해!" 라고 알리는 방송
    // EconomyConfigSynced: UGS 서버의 상점/재화 카탈로그 다운로드가 끝났음을 알리는 방송
    public event Action<PlayerEconomyData> PlayerEconomyUpdated;
    public event Action EconomyConfigSynced;

    public void Init()
    {
        // 유니티 서비스 초기화가 끝났다는 방송을 들으면 SetupBindings를 실행하도록 구독합니다.
        // 이중 구독을 막기 위해 빼기(-=)를 먼저 해준 것은 아주 훌륭한 습관입니다!
        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
        Managers.Initialize.OnUnityServiceInit += SetupBindings;
    }
    public void Clear()
    {
        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
    }
    private void SetupBindings()
    {
        // 바인딩 객체가 없다면 새로 생성하여 서버와 연결할 준비를 마칩니다.
        if (playerEconomyServiceBindings == null)
        {
            playerEconomyServiceBindings = new PlayerEconomyServiceBindings(CloudCodeService.Instance);
        }
        // 로그인 성공 시점에 상점/재화 카탈로그(설정)를 동기화하도록 구독합니다
        AuthenticationService.Instance.SignedIn -= SyncEconomyConfig;
        AuthenticationService.Instance.SignedIn += SyncEconomyConfig;
    }
    // [6] 카탈로그 동기화 함수
    // 내가 얼마를 가지고 있는지(Balance)가 아니라, 이 게임에 어떤 화폐가 존재하고 
    // 어떤 아이템을 파는지(Config)를 UGS 서버에서 다운로드 받는 아주 중요한 과정입니다.
    private async void SyncEconomyConfig()
    {
        try
        {
            await EconomyService.Instance.Configuration.SyncConfigurationAsync();
            Debug.Log("Economy configuration synced (상점 카탈로그 다운로드 완료)");

            // 카탈로그 다운로드가 끝나면 상점 UI 등에게 방송을 보냅니다.
            EconomyConfigSynced?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    // [7] 지갑 갱신 함수 (유일한 데이터 수정 창구)
    // 서버 통신(로그인, 결제 등)이 끝난 후, 서버가 "너 최신 데이터 이거야" 하고 던져주면
    // 이 함수가 받아서 지갑(EconomyDataLocal)을 덮어씌우고 방송을 터뜨립니다.
    public void HandleEconomyUpdate(PlayerEconomyData economyData)
    {
        EconomyDataLocal = economyData;
        PlayerEconomyUpdated?.Invoke(EconomyDataLocal);
    }

    // [8] 재화 조회 유틸리티 함수
    // 내 지갑에서 특정 재화(예: 다이아)가 얼마나 있는지 물어볼 때 사용합니다.
    public int GetCurrencyAmount(string currencyKey)
    {
        // TryGetValue를 써서 만약 지갑에 해당 재화 기록이 아예 없어도 
        // 뻗지 않고 안전하게 0을 반환하도록 처리한 훌륭한 방어 코드입니다.
        if (EconomyDataLocal.Currencies.TryGetValue(currencyKey,out int amount))
        {
            return amount;
        }
        return 0;
    }
}
