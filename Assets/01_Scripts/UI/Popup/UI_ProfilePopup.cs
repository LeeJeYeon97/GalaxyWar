using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class UI_ProfilePopup : UI_Popup
{
    enum Buttons
    {
        Button_Exit,
        Button_LinkAccount,
        
    }
    enum Texts
    {
        Text_NickName,
        Text_PlayerID,
    }
    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));

        GetButton((int)Buttons.Button_Exit).onClick.AddListener(OnClickExitButton);
        GetButton((int)Buttons.Button_LinkAccount).onClick.AddListener(OnClickLinkButton);

        TextSetting();
        RefreshLinkButton();
    }
    private void TextSetting()
    {
        // 1. 유니티 인증 서비스에서 정보 가져오기
        string fullName = AuthenticationService.Instance.PlayerName;
        string playerId = AuthenticationService.Instance.PlayerId;

        // 2. 닉네임 텍스트 설정 (상세 창이므로 태그까지 자르지 않고 전부 보여줍니다!)
        if (string.IsNullOrEmpty(fullName))
        {
            GetTMP((int)Texts.Text_NickName).text = "새로운 모험가";
        }
        else
        {
            // 예: "신병4321#1A2B" 원본 그대로 출력
            GetTMP((int)Texts.Text_NickName).text = fullName;
        }

        // 3. PlayerID 텍스트 설정
        GetTMP((int)Texts.Text_PlayerID).text = $"{playerId}";
    }

    //구글 연동 상태를 체크하고 버튼 UI를 업데이트하는 함수
    private void RefreshLinkButton()
    {
        Button linkBtn = GetButton((int)Buttons.Button_LinkAccount);
        TMP_Text linkBtnText = linkBtn.GetComponentInChildren<TMP_Text>();

        // CCTV 1번: TMP_Text를 제대로 찾았는가?
        if (linkBtnText == null)
        {
            Debug.LogError("비상! 연동 버튼의 자식 오브젝트에서 TMP_Text를 찾지 못했습니다! (일반 Text를 쓰고 있거나 비활성화 상태일 수 있음)");
        }

        bool isLinked = false;

        //  CCTV 2번: 서버에서 내 정보를 제대로 들고 왔는가?
        if (AuthenticationService.Instance.PlayerInfo == null)
        {
            Debug.LogError("비상! PlayerInfo가 null입니다! 정보 갱신이 안 됐습니다.");
        }
        else if (AuthenticationService.Instance.PlayerInfo.Identities == null)
        {
            Debug.LogError(" 비상! PlayerInfo.Identities가 null입니다! 연결된 계정 목록을 불러오지 못했습니다.");
        }
        else
        {
            Debug.Log($" 현재 내 계정에 연결된 총 정보(Identity) 개수: {AuthenticationService.Instance.PlayerInfo.Identities.Count}");

            foreach (var identity in AuthenticationService.Instance.PlayerInfo.Identities)
            {
                // CCTV 3번: 서버가 알려준 내 연결 타입이 정확히 무슨 글자인가?
                Debug.Log($"발견된 연결 계정 타입: [{identity.TypeId}]");

                // 만약 유니티 버전이나 환경에 따라 "google.play.games"가 아니라 "google"로 넘어올 수도 있어서 조건을 추가했어!
                if (identity.TypeId == "google-play-games" || identity.TypeId == "google.play.games" || identity.TypeId == "google")
                {
                    isLinked = true;
                    break;
                }
            }
        }

        Debug.Log($"최종 판정: isLinked = {isLinked}");

        if (isLinked)
        {
            linkBtn.interactable = false; // 버튼 클릭 비활성화 (회색으로 변함)
            if (linkBtnText != null) linkBtnText.text = "연동 완료";
        }
        else
        {
            linkBtn.interactable = true; // 버튼 클릭 활성화
            if (linkBtnText != null) linkBtnText.text = "구글 연동하기";
        }
    }
    public void OnClickLinkButton()
    {
        // 1. 유저가 연동 중에 또 누르지 못하게 임시로 막고 텍스트 변경
        Button linkBtn = GetButton((int)Buttons.Button_LinkAccount);
        linkBtn.interactable = false;
        linkBtn.GetComponentInChildren<TMP_Text>().text = "연동 진행 중...";

        // 2. LoginManager의 연동 성공 이벤트를 구독합니다. (중복 구독 방지를 위해 뺐다가 넣기)
        Managers.Login.OnLoginSuccess -= OnLinkSuccess;
        Managers.Login.OnLoginSuccess += OnLinkSuccess;

        // 3. 구글 연동 시작!
        Managers.Login.StartSignInWithGooglePlayGames();
    }
    private void OnLinkSuccess()
    {
        // 볼일이 끝났으니 이벤트 구독 해제 (메모리 누수 방지)
        Managers.Login.OnLoginSuccess -= OnLinkSuccess;

        // 연동되면서 구글 닉네임으로 덮어씌워졌을 수 있으니 텍스트 다시 세팅
        TextSetting();

        // 버튼을 "연동 완료" 상태로 영구 변경
        RefreshLinkButton();

        // 3. 성공 팝업 띄우기! 
        // (유저님이 만들어두신 시스템/알림 팝업 UI 클래스 이름으로 교체해서 사용하세요)
        Managers.UI.ShowPopupUI<UI_SystemPopup>().SetText("구글 계정 연동이 완료되었습니다!");
    }
    public void OnClickExitButton()
    {
        ClosePopupUI();
    }

    // 2. 닉네임 변경 (유저가 입력창에 이름을 쓰고 '확인'을 눌렀을 때 호출)
    public async void UpdateNickname(string newName)
    {
        try
        {
            // 유니티 인증 서버에 닉네임 저장 요청
            await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            Debug.Log("닉네임 변경 성공!");
            //RefreshProfile();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogError($"닉네임 변경 실패: {ex.Message}");
        }
    }
}
