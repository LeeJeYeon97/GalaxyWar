using DG.Tweening;
using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_LobbyScene : UI_Scene
{
    enum Buttons
    {
        Button_ShopPanel,
        Button_MainPanel,
        Button_InfoPanel,
        Button_SettingPanel,
        Button_RankingPanel,
        Button_GameStart,

    }
    public enum Panels
    {
        Panel_Shop,
        Panel_Info,
        Panel_Main,
        Panel_Setting,
        Panel_Rank,
    }

    public List<GameObject> PanelList = new List<GameObject>();

    private Panels _currentPanel = Panels.Panel_Main;

    private float _slideDistance = 1080f;

    private void Start()
    {
        Init();
    }
    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Bind<GameObject>(typeof(Panels));

        PanelList.Clear();

        foreach (Panels panel in System.Enum.GetValues(typeof(Panels)))
        {
            // GetObject를 이용해 바인딩된 게임오브젝트를 가져옵니다.
            GameObject panelGo = GetObject((int)panel);
            if (panelGo != null)
            {
                PanelList.Add(panelGo);

                // ★ 초기 세팅: Main 패널 빼고는 다 끄고 시작!
                if (panel == _currentPanel)
                {
                    panelGo.SetActive(true);
                    panelGo.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                }
                else
                {
                    panelGo.SetActive(false);
                }
            }
        }

        ButtonSetting();
    }
    public void ButtonSetting()
    {
        GetButton((int)Buttons.Button_GameStart).onClick.AddListener(OnClickStartButton);
        GetButton((int)Buttons.Button_ShopPanel).onClick.AddListener(() => ShowPanel(Panels.Panel_Shop));
        GetButton((int)Buttons.Button_SettingPanel).onClick.AddListener(() => ShowPanel(Panels.Panel_Setting));
        GetButton((int)Buttons.Button_MainPanel).onClick.AddListener(() => ShowPanel(Panels.Panel_Main));

    }
    private void OnClickStartButton()
    {
        Managers.Sound.Play(SoundID.Sfx_UIButtonClick);
        Managers.Scene.LoadScene(Define.Scene.GameScene);
    }

    public void ShowPanel(Panels targetPanel)
    {
        // 1. 이미 띄워져 있는 패널의 버튼을 또 누르면 무시!
        if (_currentPanel == targetPanel) return;

        // ★ 2. 방향 계산하기 (핵심 로직)
        int currentIndex = (int)_currentPanel;
        int targetIndex = (int)targetPanel;

        // 타겟이 현재보다 오른쪽에 있으면 1, 왼쪽에 있으면 -1 이 됩니다.
        // 예: Main(2) -> Setting(3) = 1 (오른쪽으로 이동)
        // 예: Main(2) -> Shop(0) = -1 (왼쪽으로 이동)
        float dir = targetIndex > currentIndex ? 1f : -1f;

        // 3. 게임 오브젝트 및 RectTransform 가져오기
        GameObject currentGo = PanelList[currentIndex];
        GameObject targetGo = PanelList[targetIndex];

        RectTransform currentRect = currentGo.GetComponent<RectTransform>();
        RectTransform targetRect = targetGo.GetComponent<RectTransform>();

        // (꿀팁) 유저가 버튼을 다다닥! 눌렀을 때 애니메이션이 꼬이는 것을 방지
        currentRect.DOKill();
        targetRect.DOKill();

        // --- [현재 패널 퇴장 연출] ---
        // 타겟이 오른쪽(1)에 있다면, 나는 왼쪽(-1920)으로 비켜줘야 합니다. (-_slideDistance * dir)
        currentRect.DOAnchorPosX(-_slideDistance * dir, 0.4f)
            .SetEase(Ease.OutQuart)
            .OnComplete(() => currentGo.SetActive(false));

        // --- [새로운 패널 입장 연출] ---
        targetGo.SetActive(true);

        // 타겟이 오른쪽(1)에 있다면, 오른쪽(1920)에서 출발해서 0으로 들어와야 합니다.
        targetRect.anchoredPosition = new Vector2(_slideDistance * dir, 0f);

        targetRect.DOAnchorPosX(0f, 0.4f)
            .SetEase(Ease.OutQuart);

        // 4. 현재 패널 업데이트
        _currentPanel = targetPanel;
    }
}
