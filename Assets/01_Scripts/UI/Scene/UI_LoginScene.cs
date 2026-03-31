using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_LoginScene : UI_Scene
{
    enum Texts
    {
        loadingText,
        touchToStartText,
    }
    enum Buttons
    {
        StartButton,
        Button_LoginGoogle
    }

    TMP_Text loadingText;
    TMP_Text touchToStartText;
    Button backgroundButton;

    private void Start()
    {
        Init();
    }

    public override void Init()
    {
        base.Init();
        Bind<TMP_Text>(typeof(Texts));
        Bind<Button>(typeof(Buttons));

        loadingText = GetTMP((int)Texts.loadingText);
        loadingText.gameObject.SetActive(true);
        touchToStartText = GetTMP((int)Texts.touchToStartText);
        touchToStartText.gameObject.SetActive(false);

        backgroundButton = GetButton((int)Buttons.StartButton);
        backgroundButton.onClick.AddListener(OnStartButtonClicked);
        backgroundButton.gameObject.SetActive(false);

        GetButton((int)Buttons.Button_LoginGoogle).onClick.AddListener(Managers.Login.StartSignInWithGooglePlayGames);

        Managers.Login.OnLoginSuccess += OnLoginFinished;

        if (Managers.Login.IsLoginFinished == true)
        {
            // 이미 로그인이 끝났다면 바로 UI를 갱신해 버립니다.
            OnLoginFinished();
        }
    }
    // LoginManager에서 로그인이 성공하면 자동으로 이 함수가 실행됩니다!
    private void OnLoginFinished()
    {
        Debug.Log("타이틀 씬: 로그인 완료 확인! 터치 대기 상태로 전환합니다.");

        loadingText.gameObject.SetActive(false);
        touchToStartText.gameObject.SetActive(true); // "터치하세요" 글씨가 짠! 나타남
        backgroundButton.gameObject.SetActive(true);    //버튼 터치

    }
    private void OnStartButtonClicked()
    {
        Debug.Log("start button touch");
        Managers.Scene.LoadScene(Define.Scene.LobbyScene);
    }
}
