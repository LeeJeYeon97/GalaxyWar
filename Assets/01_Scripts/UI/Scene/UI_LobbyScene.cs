using DG.Tweening;
using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.Services.CloudCode.GeneratedBindings.Project;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_LobbyScene : UI_Scene
{
    enum Buttons
    {
        Button_ShopPanel,
        Button_MainPanel,
        Button_RankingPanel,
        Button_Profile,
        Button_Setting,
    }
    public enum Panels
    {
        UI_ShopPanel,
        UI_MainPanel,
        
    }
    enum Texts
    {
        Text_Coins
    }

    public List<GameObject> PanelList = new List<GameObject>();

    private Panels _currentPanel = Panels.UI_MainPanel;

    private float _slideDistance = 1080f;

    private Buttons _currentTabButton = Buttons.Button_MainPanel;

    public GameObject _LobbyObject;

    public override void Init()
    {
        base.Init();

        // 1. 이 UI에 붙어있는 Canvas 컴포넌트를 가져옵니다.
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            // 2. 렌더 모드를 Screen Space - Camera로 변경합니다.
            canvas.renderMode = RenderMode.ScreenSpaceCamera;

            // 3. 현재 씬의 메인 카메라를 찾아서 캔버스에 꽂아줍니다.
            // (Camera.main은 태그가 "MainCamera"로 설정된 카메라를 자동으로 찾아옵니다)
            canvas.worldCamera = Camera.main;

            // 4. 카메라와 UI 사이의 거리(Plane Distance)를 설정합니다.
            // 이 공간 사이에 파티클이 들어가서 터져야 하므로 넉넉하게 10~50 정도를 줍니다.
            canvas.planeDistance = 20f;
        }
        _LobbyObject = GameObject.Find("_LobbyObject");
        _LobbyObject.SetActive(false);

        Bind<Button>(typeof(Buttons));
        Bind<GameObject>(typeof(Panels));
        Bind<TMP_Text>(typeof(Texts));
        
        SetPanels();
        ButtonSetting();

        Managers.PlayerEconomy.PlayerEconomyUpdated -= UpdateCoinsText;
        Managers.PlayerEconomy.PlayerEconomyUpdated += UpdateCoinsText;

        if (Managers.PlayerData.PlayerDataLocal != null)
        {
            UpdateCoinsText(Managers.PlayerEconomy.EconomyDataLocal);
        }
    }
    public override void Clear()
    {
        Managers.PlayerEconomy.PlayerEconomyUpdated -= UpdateCoinsText;
    }
    private void UpdateCoinsText(PlayerEconomyData data)
    {
        int amount = data.Currencies[Define.k_GoldCurrencyKey];
        GetTMP((int)Texts.Text_Coins).text = amount.ToString("N0");
    }
    public void ShowPanel(Panels targetPanel)
    {
        // 1. 이미 띄워져 있는 패널의 버튼을 또 누르면 무시!
        if (_currentPanel == targetPanel) return;

        //  2. 방향 계산하기 (핵심 로직)
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

        if(_currentPanel == Panels.UI_MainPanel)
            _LobbyObject.SetActive(false);

        currentRect.DOAnchorPosX(-_slideDistance * dir, 0.4f)
            .SetEase(Ease.OutQuart)
            .OnComplete(() =>
            {
                currentGo.SetActive(false);
            });

        // --- [새로운 패널 입장 연출] ---
        targetGo.SetActive(true);

        
        // 타겟이 오른쪽(1)에 있다면, 오른쪽(1920)에서 출발해서 0으로 들어와야 합니다.
        targetRect.anchoredPosition = new Vector2(_slideDistance * dir, 0f);

        targetRect.DOAnchorPosX(0f, 0.4f)
            .SetEase(Ease.OutQuart)
            .OnComplete(()=>
            { 
                if (targetPanel == Panels.UI_MainPanel) 
                    _LobbyObject.SetActive(true);
            });

        // 하단 탭 버튼 크기 애니메이션 로직
        Buttons targetButton = GetTabButtonByPanel(targetPanel);

        // 1. 다다닥 눌렀을 때 꼬이는 것을 방지하기 위해 킬(Kill)
        GetButton((int)_currentTabButton).transform.DOKill();
        GetButton((int)targetButton).transform.DOKill();

        // 2. 기존에 선택되어 있던 버튼은 1.0배로 줄어듬
        GetButton((int)_currentTabButton).transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);

        // 3. 새로 누른 버튼은 1.1배로 커짐 (OutBack을 쓰면 살짝 튕기면서 커져서 이쁩니다!)
        GetButton((int)targetButton).transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack);

        // 4. 상태 업데이트
        _currentTabButton = targetButton;
        _currentPanel = targetPanel;
    }
    private void SetPanels()
    {
        PanelList.Clear();

        foreach (Panels panel in System.Enum.GetValues(typeof(Panels)))
        {
            // GetObject를 이용해 바인딩된 게임오브젝트를 가져옵니다.
            GameObject panelGo = GetObject((int)panel);
            if (panelGo != null)
            {
                PanelList.Add(panelGo);

                // 초기 세팅: Main 패널 빼고는 다 끄고 시작!
                if (panel == _currentPanel)
                {
                    panelGo.SetActive(true);
                    _LobbyObject.SetActive(true);
                    panelGo.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                }
                else
                {
                    panelGo.SetActive(false);
                }
            }
        }

    }
    public void ButtonSetting()
    {

        GetButton((int)Buttons.Button_ShopPanel).onClick.AddListener(() => ShowPanel(Panels.UI_ShopPanel));
        GetButton((int)Buttons.Button_MainPanel).onClick.AddListener(() => ShowPanel(Panels.UI_MainPanel));

        GetButton((int)Buttons.Button_Profile).onClick.AddListener(() => Managers.UI.ShowPopupUI<UI_ProfilePopup>());
        GetButton((int)Buttons.Button_Setting).onClick.AddListener(() => Managers.UI.ShowPopupUI<UI_SettingsPopup>());

        GetButton((int)_currentTabButton).transform.localScale = Vector3.one * 1.1f;

    }
    private Buttons GetTabButtonByPanel(Panels panel)
    {
        switch (panel)
        {
            case Panels.UI_ShopPanel: return Buttons.Button_ShopPanel;
            case Panels.UI_MainPanel: return Buttons.Button_MainPanel;
            //case Panels.UI_RankPanel: return Buttons.Button_RankingPanel;
            default: return Buttons.Button_MainPanel;
        }
    }
}
