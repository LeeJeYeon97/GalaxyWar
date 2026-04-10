using DG.Tweening; // DOTween
using System.Runtime.Serialization;
using TMPro;

using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_GameScene : UI_Scene
{
    enum Texts
    {
        LevelText,
        ScoreText,
        BurstModeText,
        TimeText
    }
    enum Sliders
    {
        HpBar,
        ShieldBar,
        ExpBar,
    }
    enum Buttons
    {
        RestartButton,
        PauseButton,
        BurstModeBar,
    }
    enum Images
    {
        BurstModeBar,
        BurstModeLock,
    }

    private TMP_Text timeText;

    public override void Clear()
    {
        Managers.Event.UnSubscribe<int>(ActionEvent.LevelUp, UpdateLevelText);
        Managers.Event.UnSubscribe<(float curExp, float maxExp)>(ActionEvent.ExpChanged, UpdateExpBar);
        Managers.Event.UnSubscribe<float>(ActionEvent.ScoreChanged, UpdateScoreText);

        Managers.Event.UnSubscribe<PlayerStatusEvent>(ActionEvent.PlayerStatusChanged, UpdateHUD);
        Managers.Event.UnSubscribe(ActionEvent.EnableBurstMode, EnableBurstButton);

        Managers.Event.UnSubscribe<float>(ActionEvent.UpdateGameTime, UpdateGameTime);
    }
    public override void Init()
    {
        base.Init();

        Bind<TMP_Text>(typeof(Texts));
        Bind<Slider>(typeof(Sliders));
        Bind<Button>(typeof(Buttons));
        Bind<Image>(typeof(Images));

        Get<TMP_Text>((int)Texts.BurstModeText).gameObject.SetActive(false);
        Get<Image>((int)Images.BurstModeLock).gameObject.SetActive(true);

        timeText = Get<TMP_Text>((int)Texts.TimeText);

        Canvas canvas = Util.GetOrAddComponent<Canvas>(this.gameObject);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;

        Managers.Event.Subscribe<(float curExp, float maxExp)>(ActionEvent.ExpChanged, UpdateExpBar);
        Managers.Event.Subscribe<int>(ActionEvent.LevelUp, UpdateLevelText);
        Managers.Event.Subscribe<float>(ActionEvent.ScoreChanged, UpdateScoreText);

        Managers.Event.Subscribe<PlayerStatusEvent>(ActionEvent.PlayerStatusChanged, UpdateHUD);
        Managers.Event.Subscribe(ActionEvent.EnableBurstMode, EnableBurstButton);
        Managers.Event.Subscribe<float>(ActionEvent.UpdateGameTime, UpdateGameTime);

        BindingButtonClickListener();

        


    }
    private void BindingButtonClickListener()
    {
        Button restartButton = GetButton((int)Buttons.RestartButton);
        restartButton.onClick.AddListener(OnClickGameTestButton);

        Button BurstButton = GetButton((int)Buttons.BurstModeBar);
        BurstButton.onClick.AddListener(OnBurstButton);

        Button PauseButton = GetButton((int)Buttons.PauseButton);
        PauseButton.onClick.AddListener(OnClickPauseButton);
    }
    public void UpdateHUD(Define.PlayerStatusEvent data)
    {
        Slider hpSlider = GetSlider((int)Sliders.HpBar);
        Slider shieldSlider = GetSlider((int)Sliders.ShieldBar);
        Image burstBar = GetImage((int)Images.BurstModeBar);
        Slider expSlider = GetSlider((int)Sliders.ExpBar);

        if (hpSlider == null || shieldSlider == null || burstBar == null || expSlider == null)
            return;

        // 체력바
        hpSlider.DOKill();
        hpSlider.DOValue(data.hp / data.maxHp, 0.2f).SetEase(Ease.OutCubic);

        // 쉴드바
        shieldSlider.DOKill();
        shieldSlider.DOValue(data.shield / data.maxShield, 0.2f).SetEase(Ease.OutCubic);

        
        burstBar.DOKill();
        burstBar.DOFillAmount(data.burst / data.maxBurst, 0.2f).SetEase(Ease.OutCubic);


        // 4. 버스트 텍스트 업데이트
        TMP_Text burstText = GetTMP((int)Texts.BurstModeText);
        if (burstText != null)
        {
            burstText.text = Mathf.FloorToInt(data.burst).ToString();
        }

    }
    public void EnableBurstButton()
    {
        // 버스트 모드 활성화 되었을 때 한번 실행됨
        Get<Image>((int)Images.BurstModeLock).gameObject.SetActive(false);
        Get<TMP_Text>((int)Texts.BurstModeText).gameObject.SetActive(true);
    }
    public void OnBurstButton()
    {
        // 버스트 모드 실행
        Managers.Game._player?.ActivateBurst();
    }
    public void UpdateLevelText(int level)
    {
        // 레벨 텍스트 갱신
        Slider slider = GetSlider((int)Sliders.ExpBar);
        slider.value = 0;
        string text = $"Lv.{level}";
        GetTMP((int)Texts.LevelText).text = text;
    }
    public void UpdateScoreText(float Score)
    {
        string text = $"{Score}";
        GetTMP((int)Texts.ScoreText).text = text;
    }
    public void UpdateExpBar((float curExp, float maxExp) data)
    {
        Slider expSlider = GetSlider((int)Sliders.ExpBar);
        if (expSlider == null)
            return;

        expSlider.DOKill();
        // exp바
        expSlider.DOValue(data.curExp / data.maxExp, 0.2f).SetEase(Ease.OutCubic);
    }
    public void OnClickGameTestButton()
    {
        Managers.Sound.Play(SoundID.Sfx_UIButtonClick, Sound.Sfx);
        Managers.Game.TestAbility();
    }

    private void OnClickPauseButton()
    {
        Managers.Game.ChangeGameState(GameState.Pause);
        Managers.UI.ShowPopupUI<UI_PausePopup>();
    }

    private void UpdateGameTime(float time)
    {
        // 1. float 시간을 분(int)과 초(int)로 쪼갭니다.
        int minutes = Mathf.FloorToInt(time / 60f); // 60으로 나눈 몫 (분)
        int seconds = Mathf.FloorToInt(time % 60f); // 60으로 나눈 나머지 (초)

        // 2. 분과 초를 "00:00" 형식의 문자열로 만듭니다.
        // (예: 5분 3초 -> "05:03")
        string timeString = $"{minutes:00}:{seconds:00}";

        // 3. 텍스트 UI에 덮어씌웁니다.
        if (timeText != null)
        {
            timeText.text = timeString;
        }
    }
    
}
