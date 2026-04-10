using DG.Tweening;
using TMPro;
using Unity.Services.CloudCode.GeneratedBindings.Project;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UI_LoginScene : UI_Scene
{
    enum Texts
    {
        loadingText,
    }
    enum Buttons
    {
        StartButton,
        Button_LoginGoogle
    }
    enum Sliders
    {
        LoadingBar,
    }

    TMP_Text loadingText;
    Button backgroundButton;
    Slider loadingBar;

    public override void Init()
    {
        base.Init();
        Bind<TMP_Text>(typeof(Texts));
        Bind<Button>(typeof(Buttons));
        Bind<Slider>(typeof(Sliders));

        loadingText = GetTMP((int)Texts.loadingText);
        loadingText.gameObject.SetActive(true);

        backgroundButton = GetButton((int)Buttons.StartButton);
        backgroundButton.onClick.AddListener(OnStartButtonClicked);
        backgroundButton.gameObject.SetActive(false);

        loadingBar = GetSlider((int)Sliders.LoadingBar);
        loadingBar.value = 0f;

        // 1. 초기화 진행 상황 구독
        Managers.Initialize.OnInitProgress -= UpdateProgress;
        Managers.Initialize.OnInitProgress += UpdateProgress;

        // 2. 로그인 성공 시 진행 상황 업데이트 구독 (0.7f 갱신용)
        Managers.Login.OnLoginSuccess -= OnLoginSuccessProgress;
        Managers.Login.OnLoginSuccess += OnLoginSuccessProgress;

        // 3. 진짜 마지막! 플레이어 데이터가 다 불러와졌을 때 화면 활성화
        Managers.PlayerEconomy.PlayerEconomyUpdated -= OnInitFinished;
        Managers.PlayerEconomy.PlayerEconomyUpdated += OnInitFinished;

        // 예외 처리: 이미 로딩이 다 끝난 상태에서 UI가 켜졌다면 바로 완료 처리
        if (Managers.PlayerEconomy.EconomyDataLocal != null)
        {
            OnInitFinished(Managers.PlayerEconomy.EconomyDataLocal);
        }
    }
    public override void Clear()
    {
        base.Clear(); // 부모의 Clear도 혹시 모르니 불러주고

        loadingBar.DOKill();

        if (Managers.Login != null)
        {
            Managers.Login.OnLoginSuccess -= OnLoginSuccessProgress;
        }
        if (Managers.PlayerEconomy != null)
        {
            Managers.PlayerEconomy.PlayerEconomyUpdated -= OnInitFinished;
        }
        Managers.Initialize.OnInitProgress -= UpdateProgress;
    }

    // LoginManager가 끝났을 때: 아직 끝난 게 아닙니다! 로딩바만 올려줍니다.
    private void OnLoginSuccessProgress()
    {
        UpdateProgress(0.7f, "플레이어 데이터 동기화 중...");
    }

    // PlayerDataManager가 끝났을 때: 진짜 로딩 완료! (PlayerData 매개변수 필요)
    private void OnInitFinished(PlayerEconomyData data)
    {
        loadingBar.DOValue(1f, 0.5f).OnComplete(() =>
        {
            loadingText.text = "아무 곳이나 터치하여 시작";
            backgroundButton.gameObject.SetActive(true);
        });
    }
    public void UpdateProgress(float progress, string text)
    {
        loadingBar.DOValue(progress, 0.5f);

        if (text != null)
        {
            loadingText.text = text;
        }
    }

    private void OnStartButtonClicked()
    {
        Managers.Scene.LoadScene(Define.Scene.LobbyScene);
    }
}
