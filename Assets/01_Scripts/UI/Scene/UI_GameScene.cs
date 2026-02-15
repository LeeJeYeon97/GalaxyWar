using DG.Tweening; // DOTween
using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class UI_GameScene : UI_Scene
{
    enum Texts
    {
        LevelText,
        ScoreText,
        BurstModeText
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
        BurstModeButton,
    }
    enum Images
    {
        BurstModeBar,
    }
    
    public void Start()
    {
        Init();
    }
    private void OnEnable()
    {
        Managers.Level.OnExpChanged += UpdateExpBar;
        Managers.Level.OnLevelUp += UpdateLevelText;
        Managers.Game.OnUpdateScore += UpdateScoreText;

        Managers.Game._player.OnHpChanged += UpdateHpBar;
        Managers.Game._player.OnBurstChanged += UpdateBurstBar;
        Managers.Game._player.OnDefenceChanged += UpdateShieldBar;
    }
    private void OnDisable()
    {
        Managers.Level.OnExpChanged -= UpdateExpBar;
        Managers.Level.OnLevelUp -= UpdateLevelText;
        Managers.Game.OnUpdateScore -= UpdateScoreText;

        Managers.Game._player.OnHpChanged -= UpdateHpBar;
        Managers.Game._player.OnBurstChanged -= UpdateBurstBar;
        Managers.Game._player.OnDefenceChanged -= UpdateShieldBar;
    }
    public override void Init()
    {
        base.Init();

        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Slider>(typeof(Sliders));
        Bind<Button>(typeof(Buttons));
        Bind<Image>(typeof(Images));

        Canvas canvas = Util.GetOrAddComponent<Canvas>(this.gameObject);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;

        UpdateSlider();

        Button restartButton = GetButton((int)Buttons.RestartButton);
        restartButton.onClick.AddListener(OnClickGameTestButton);
        Button PauseButton = GetButton((int)Buttons.PauseButton);

        Button BurstButton = GetButton((int)Buttons.BurstModeButton);
        BurstButton.onClick.AddListener(Managers.Game._player.ActivateBurst);
    }
    private void UpdateSlider()
    {
        UpdateExpBar(Managers.Level.CurrentExp, Managers.Level.MaxExp);
        UpdateLevelText(Managers.Level.CurrentLevel);
        UpdateScoreText(Managers.Game.Score);
        UpdateHpBar(Managers.Game._player.currentHp, Managers.Game._player.stat.maxHp.TotalValue);
        UpdateShieldBar(Managers.Game._player.currentDefence, Managers.Game._player.stat.maxDefence.TotalValue);
        //UpdateBurstBar(Managers.Game._player., Managers.Game._player.maxBurst);
    }
    public void UpdateExpBar(float curExp, float maxExp)
    {
        Slider slider = GetSlider((int)Sliders.ExpBar);

        if (slider == null)
            return;

        slider.DOValue(curExp / maxExp, 0.5f).SetEase(Ease.OutCubic);
    }
    
    public void UpdateBurstBar(float curBurst, float maxBurst)
    {
        Image image = GetImage((int)Images.BurstModeBar);

        string text = Mathf.FloorToInt(curBurst).ToString();
        GetTMP((int)Texts.BurstModeText).text = text;

        if (image == null)
            return;

        float ratio = curBurst / maxBurst;

        // DOFillAmount(목표값, 시간) 사용
        image.DOKill(); // 이전 애니메이션이 실행 중이면 중지
        image.DOFillAmount(ratio, 0.5f).SetEase(Ease.OutCubic);
    }
    public void UpdateHpBar(float curHp, float maxHp)
    {
        Slider slider = GetSlider((int)Sliders.HpBar);

        if (slider == null)
            return;

        slider.DOValue(curHp / maxHp, 0.5f).SetEase(Ease.OutCubic);
    }
    public void UpdateShieldBar(float curShield, float maxShield)
    {
        Slider slider = GetSlider((int)Sliders.ShieldBar);

        if (slider == null)
            return;

        slider.DOValue(curShield / maxShield, 0.5f).SetEase(Ease.OutCubic);
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
    public void OnClickGameTestButton()
    {
        Managers.Game.TestAbility();
    }
}
