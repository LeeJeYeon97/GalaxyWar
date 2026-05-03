using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using static Define;

public class UI_GameOverPopup : UI_Popup
{
    enum Buttons
    {  
        Btn_Restart,
        Btn_RewardAD,
        Btn_QuitLobby
    }
    enum Texts
    {
        RewardCountText,
    }

    private Button _btnReviveAd;
    private TMP_Text _txtReviveCount;

    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));

        GetButton((int)Buttons.Btn_Restart).onClick.AddListener(OnClickRestartButton);
        GetButton((int)Buttons.Btn_QuitLobby).onClick.AddListener(OnClickQuitLobbyButton);

        _txtReviveCount = GetTMP((int)Texts.RewardCountText);

        _btnReviveAd = GetButton((int)Buttons.Btn_RewardAD);
        _btnReviveAd.onClick.AddListener(OnClickRewardADButton);

        RefreshRewardCountText();
    }
    //유니티 눈치 안 보고 내가 원할 때 직접 번역본을 가져오는 마법의 함수!
    private void RefreshRewardCountText()
    {
        int remainCount = Managers.Game.reviveCount;

        // 1. 텍스트에 붙어있는 LocalizeStringEvent 컴포넌트를 가져옵니다.
        var localizeEvent = _txtReviveCount.GetComponent<LocalizeStringEvent>();

        if (localizeEvent != null)
        {
            // 2. 컴포넌트의 번역 데이터({0} 자리)에 들어갈 값을 object 배열로 넘겨줍니다.
            // 만약 {0}, {1} 두 개라면 new object[] { remainCount, otherValue } 처럼 넣으면 됩니다.
            localizeEvent.StringReference.Arguments = new object[] { remainCount };

            // 3. 컴포넌트에게 "인자가 들어왔으니 텍스트 다시 그려!" 라고 명령합니다.
            // 이때 컴포넌트가 알아서 내부적으로 string.Format을 실행해 예쁘게 출력합니다.
            localizeEvent.RefreshString();
        }
        else
        {
            // (혹시 에디터에서 실수로 컴포넌트를 지웠을 때를 대비한 안전장치)
            string localizedText = Util.GetLocalizeString("UI", "GameOverPopup_ReviveCount");
            if (string.IsNullOrEmpty(localizedText)) localizedText = "남은 횟수 : {0}";
            _txtReviveCount.text = string.Format(localizedText, remainCount);
        }

        // 4. 남은 횟수가 없으면 광고 버튼 비활성화
        if (remainCount <= 0)
        {
            _btnReviveAd.interactable = false;
        }
        else
        {
            _btnReviveAd.interactable = true;
        }
    }

    private async void OnClickRestartButton()
    {
        // 1. 중복 클릭 방지를 위해 버튼 비활성화
        GetButton((int)Buttons.Btn_Restart).interactable = false;

        // 2. 서버 데이터 저장 완료까지 대기
        await SaveSessionData();

        // 현재 씬(GameScene)을 다시 로드! (가장 깔끔한 초기화)
        Managers.Scene.LoadScene(Define.Scene.GameScene);
    }
    private async void OnClickQuitLobbyButton()
    {
        // 1. 중복 클릭 방지
        GetButton((int)Buttons.Btn_QuitLobby).interactable = false;

        // 2. 서버 데이터 저장 완료까지 대기
        await SaveSessionData();

        // 로비로 돌아가기
        // 현재 씬(GameScene)을 다시 로드! (가장 깔끔한 초기화)
        Managers.AD.ShowInterstitialAd();

        Managers.Scene.LoadScene(Define.Scene.LobbyScene);
    }
    private void OnClickRewardADButton()
    {
        // 클릭 중복 방지 (광고 로딩 중 버튼 끄기)
        _btnReviveAd.interactable = false;

        // 아이언소스 보상형 광고 호출 (플레이스먼트 이름은 대시보드에 맞게 수정하세요)
        Managers.AD.ShowRewardedAd(placement_GameOver, (success) =>
        {
            if (success)
            {
                Debug.Log("부활 광고 시청 완료! 플레이어를 부활시킵니다.");

                // 1. 남은 횟수 차감
                Managers.Game.reviveCount--;

                // 2. 플레이어 부활 함수 호출 (이전에 만들어두신 PlayerController의 Revive() 활용!)
                PlayerController player = GameObject.FindFirstObjectByType<PlayerController>();
                if (player != null)
                {
                    player.SetState(PlayerState.Die); // Revive 내부 로직 통과를 위해 상태 맞춤 (필요시)
                    player.Revive();
                    player.SetState(PlayerState.Playing);
                }

                // 3. 게임 상태 복구 및 팝업 닫기
                Managers.Game.ChangeGameState(GameState.Playing);
                Managers.UI.ClosePopupUI();
            }
            else
            {
                Debug.Log("부활 광고 시청 실패 또는 취소.");

                // 광고를 중간에 껐다면 버튼을 다시 누를 수 있게 켜줍니다.
                RefreshRewardCountText();
            }
        });
    }

    private async Task SaveSessionData()
    {
        int sessionGold = Managers.Game.currentSessionGold;

        // 얻은 골드가 0보다 클 때만 서버에 요청을 보냅니다.
        if (sessionGold > 0)
        {
            Debug.Log($"서버에 {sessionGold} 골드 저장을 요청합니다...");

            // 저번에 만든 PlayerEconomyManager의 기능을 활용합니다.
            // (함수명은 프로젝트의 Economy 관리자 구현에 맞게 맞추시면 됩니다)
            bool success = await Managers.PlayerEconomy.AddGoldAsync(sessionGold);

            if (success)
                Debug.Log("골드 저장 성공!");
            else
                Debug.LogWarning("골드 저장 실패. 네트워크 상태를 확인하세요.");
        }

        // 2. 플레이어 최고 기록(점수, 생존 시간) 저장
        Debug.Log("서버에 플레이어 기록 저장을 요청합니다...");

        // PlayerDataManager에 만들어둔 저장 함수를 호출하고 끝날 때까지 기다립니다.
        await Managers.PlayerData.SavePlayerData();
    }
}
