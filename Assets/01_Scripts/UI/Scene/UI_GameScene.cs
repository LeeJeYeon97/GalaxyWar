using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween

public class UI_GameScene : UI_Scene
{
    enum Texts
    {
        //ExpText,
        LevelText,
        ScoreText,
    }
    enum Sliders
    {
        ExpBar,
    }
    enum Buttons
    {
        RestartButton,
        PauseButton,
    }

    public void UpdateExpBar(float curExp, float maxExp)
    {
        
        Slider slider = GetSlider((int)Sliders.ExpBar);

        if(slider == null)
            return;

        slider.DOValue(curExp / maxExp, 0.5f).SetEase(Ease.OutCubic);
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
    public void Start()
    {
        Init();
    }
    private void OnEnable()
    {
        Managers.Level.OnExpChanged += UpdateExpBar;
        Managers.Level.OnLevelUp += UpdateLevelText;
        Managers.Game.OnUpdateScore += UpdateScoreText;
    }
    private void OnDisable()
    {
        Managers.Level.OnExpChanged -= UpdateExpBar;
        Managers.Level.OnLevelUp -= UpdateLevelText;
        Managers.Game.OnUpdateScore -= UpdateScoreText;
    }
    public override void Init()
    {
        base.Init();

        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Slider>(typeof(Sliders));
        Bind<Button>(typeof(Buttons));

        Canvas canvas = Util.GetOrAddComponent<Canvas>(this.gameObject);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;

        UpdateExpBar(Managers.Level.CurrentExp, Managers.Level.MaxExp);
        UpdateLevelText(Managers.Level.CurrentLevel);
        UpdateScoreText(Managers.Game.Score);

        Button restartButton = GetButton((int)Buttons.RestartButton);
        restartButton.onClick.AddListener(OnClickGameTestButton);
        Button PauseButton = GetButton((int)Buttons.PauseButton);
    }

    public void OnClickGameTestButton()
    {
        Managers.Game.TestAbility();
    }
}
