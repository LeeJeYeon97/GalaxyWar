using DG.Tweening; // DOTween
using System.Collections.Generic;
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
        Text_Score,
        Text_Kill,
        Text_Gold,
        Text_Stage,
        BurstModeText,
        TimeText,
        Text_Hp,
        Text_Shield,
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

    // 4개의 고정된 자리(좌표)를 기억할 배열
    public Vector2[] _slotPositions = new Vector2[4];
    // 4번째 자리 뒤에서 대기할 '화면 밖' 좌표
    public Vector2 _spawnPosition;
    public List<Image> hudBulletImages = new List<Image>();

    private TMP_Text timeText;

    public override void Clear()
    {
        Managers.Event.UnSubscribe<int>(ActionEvent.LevelUp, UpdateLevelText);
        Managers.Event.UnSubscribe<(float curExp, float maxExp)>(ActionEvent.ExpChanged, UpdateExpBar);
        Managers.Event.UnSubscribe<float>(ActionEvent.ScoreChanged, UpdateScoreText);

        Managers.Event.UnSubscribe<PlayerStatusEvent>(ActionEvent.PlayerStatusChanged, UpdateHUD);
        Managers.Event.UnSubscribe(ActionEvent.EnableBurstMode, EnableBurstButton);

        Managers.Event.UnSubscribe<float>(ActionEvent.UpdateGameTime, UpdateGameTime);

        Managers.Event.UnSubscribe<List<BulletController>>(ActionEvent.ReloadEnd, UpdateBulletSlots);

        Managers.Event.UnSubscribe<List<BulletController>>(ActionEvent.PlayerShot, ShootAndSlide);

        Managers.Event.UnSubscribe(ActionEvent.GetGold, UpdateGoldText);

        Managers.Event.UnSubscribe(ActionEvent.MeteorDie, UpdateKillText);
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

        GetTMP((int)Texts.Text_Kill).text = Managers.Game.killCount.ToString("N0");
        GetTMP((int)Texts.Text_Gold).text = Managers.Game.currentSessionGold.ToString("N0");

        GetTMP((int)Texts.Text_Stage).text = $"STAGE {Managers.Stage.currentStageLevel:D2}";

        Canvas canvas = Util.GetOrAddComponent<Canvas>(this.gameObject);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;

        Managers.Event.Subscribe<(float curExp, float maxExp)>(ActionEvent.ExpChanged, UpdateExpBar);
        Managers.Event.Subscribe<int>(ActionEvent.LevelUp, UpdateLevelText);
        Managers.Event.Subscribe<float>(ActionEvent.ScoreChanged, UpdateScoreText);

        Managers.Event.Subscribe<PlayerStatusEvent>(ActionEvent.PlayerStatusChanged, UpdateHUD);
        Managers.Event.Subscribe(ActionEvent.EnableBurstMode, EnableBurstButton);
        Managers.Event.Subscribe<float>(ActionEvent.UpdateGameTime, UpdateGameTime);

        Managers.Event.Subscribe<List<BulletController>>(ActionEvent.PlayerShot, ShootAndSlide);
        Managers.Event.Subscribe<List<BulletController>>(ActionEvent.ReloadEnd, UpdateBulletSlots);


        Managers.Event.Subscribe(ActionEvent.GetGold, UpdateGoldText);

        Managers.Event.Subscribe(ActionEvent.MeteorDie, UpdateKillText);

        for (int i = 0; i < 4; i++)
        {
            _slotPositions[i] = hudBulletImages[i].rectTransform.anchoredPosition;
        }
        //  2. 새로 들어올 총알이 출발할 대기 위치를 계산합니다.
        // (가로로 나열되어 있다면, 3번 자리와 4번 자리의 간격만큼 뒤로 뺍니다)
        float spacing = _slotPositions[3].x - _slotPositions[2].x;

        // 4번째 자리에서 오른쪽으로 '간격'만큼 더 이동한 곳이 새로운 총알의 대기(Spawn) 위치가 됩니다!
        _spawnPosition = _slotPositions[3] + new Vector2(spacing, 0);

        // 5번째 이미지(대기석 이미지)는 시작할 때 안 보이게 숨겨두고 스폰 위치로 옮깁니다.
        hudBulletImages[4].gameObject.SetActive(false);
        hudBulletImages[4].rectTransform.anchoredPosition = _spawnPosition;

        BindingButtonClickListener();
    }
    private void BindingButtonClickListener()
    {
        //Button restartButton = GetButton((int)Buttons.RestartButton);
        //restartButton.onClick.AddListener(OnClickGameTestButton);

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

        if (hpSlider == null || burstBar == null || expSlider == null || shieldSlider == null)
            return;

        // 체력바
        hpSlider.DOKill();
        hpSlider.DOValue(data.hp / data.maxHp, 0.2f).SetEase(Ease.OutCubic);

        GetTMP((int)Texts.Text_Hp).text = $"{data.hp} / {data.maxHp}";

        // 쉴드바 처음엔 0으로
        shieldSlider.DOKill();
        float value = data.currentShieldGuage/ data.maxShieldGuage;
        shieldSlider.DOValue(value, 0.2f).SetEase(Ease.OutCubic);


        GetTMP((int)Texts.Text_Shield).text = $"{data.currentShieldGuage} / {data.maxHp}";

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
    public void UpdateKillText()
    {
        GetTMP((int)Texts.Text_Kill).text = Managers.Game.killCount.ToString("N0");
    }
    public void UpdateScoreText(float Score)
    {
        string text = $"{Score}";
        GetTMP((int)Texts.Text_Score).text = text;
    }
    public void UpdateGoldText()
    {
        GetTMP((int)Texts.Text_Gold).text = Managers.Game.currentSessionGold.ToString("N0");
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

    public void UpdateBulletSlots(List<BulletController> loadedBullets)
    {
        // UI에 보여줄 최대 슬롯 개수 (우리가 지정해둔 4개)
        int maxSlotCount = 4;

        for (int i = 0; i < maxSlotCount; i++)
        {
            // 슬롯 하나를 가져옵니다. (0번부터 3번까지)
            Image slotImage = hudBulletImages[i];

            // 만약 현재 장전된 전체 총알 개수가 i보다 크다면?
            // (ex: 장전된 총알이 10개면 i가 0~3일 때 무조건 통과!)
            if (i < loadedBullets.Count)
            {
                // 1. 슬롯을 켭니다.
                slotImage.gameObject.SetActive(true);
                // 2. 해당 순번의 총알 아이콘(선명한 흰색 벡터 그래픽)을 슬롯에 넣어줍니다.
                slotImage.sprite = loadedBullets[i].Stat.hudIcon;
            }
            else
            {
                // 장전된 총알이 2개밖에 없는데 i가 2, 3으로 넘어왔을 때 (빈자리)
                // 슬롯 자체를 꺼서 화면에서 안 보이게 숨깁니다.
                slotImage.gameObject.SetActive(false);

                // (디자인 팁: 아예 끄지 않고 빈 슬롯의 느낌을 주고 싶다면 
                // SetActive(false) 대신 투명도를 0으로 주거나, 순수 검은 배경에 맞춰 아주 어두운 회색(예: #111111)의 빈 테두리 이미지를 넣어도 예쁩니다.)
            }
        }
    }

    public void ShootAndSlide(List<BulletController> loadedBullets)
    {
        //  5개 이미지가 세팅되어 있는지 확인
        if (hudBulletImages.Count < 5) return;

        // 1. 발사될 맨 앞 슬롯(0번)을 가져옵니다.
        Image firedSlot = hudBulletImages[0];

        //  핵심: 리스트에서 맨 앞을 빼서 '즉시' 맨 뒤로 보냅니다!
        // 이제 리스트의 [0~3]번은 화면에 남을 녀석들, [4]번은 위로 날아가는 녀석이 됩니다.
        hudBulletImages.RemoveAt(0);
        hudBulletImages.Add(firedSlot);

        // 2. 발사 연출: 맨 뒤로 보내진 firedSlot은 위로만 날아갑니다. (오른쪽 이동 안 함!)
        firedSlot.DOKill();
        firedSlot.rectTransform.DOKill();

        firedSlot.rectTransform.DOAnchorPosY(firedSlot.rectTransform.anchoredPosition.y + 50f, 0.2f);
        firedSlot.DOFade(0, 0.2f).OnComplete(() =>
        {
            // 연출 끝나면 대기석(SpawnPosition)으로 조용히 순간이동 후 원상복구
            firedSlot.gameObject.SetActive(false);
            firedSlot.rectTransform.anchoredPosition = _spawnPosition;
            Color c = firedSlot.color; c.a = 1f; firedSlot.color = c;
        });

        // 3. 나머지 4개의 이미지를 원래 앞자리 좌표들로 스윽 당겨옵니다.
        // 리스트 인덱스가 당겨졌으므로, 0~3번 녀석들을 _slotPositions[0~3]으로 보냅니다.
        for (int i = 0; i < 4; i++)
        {
            Image moveSlot = hudBulletImages[i];
            moveSlot.rectTransform.DOKill();
            moveSlot.rectTransform.DOAnchorPos(_slotPositions[i], 0.2f).SetEase(Ease.OutQuart);
        }

        // 4. 대기석에서 4번째 자리(인덱스 3)로 스윽 들어오는 녀석에게 새 총알 이미지를 넣어줍니다.
        // 이 녀석은 원래 5번째 대기석에 있던 녀석입니다. (hudBulletImages[3])
        Image newSlot = hudBulletImages[3];

        if (loadedBullets.Count >= 4)
        {
            newSlot.sprite = loadedBullets[3].Stat.hudIcon;
            newSlot.gameObject.SetActive(true);
        }
        else
        {
            // 장전된 총알이 모자라면 들어올 게 없으니 끕니다.
            newSlot.gameObject.SetActive(false);
        }
    }

}
