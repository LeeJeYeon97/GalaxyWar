using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_MainPanel : UI_Base
{
    enum Buttons
    {

        Button_GameStart,
        Button_NextStage,
        Button_PreviousStage,
    }
    enum GameObjects
    {
        Card_1,
        Card_2,
    }

    [Header("슬라이드 설정")]
    public float slideDuration = 0.4f; // 슬라이드 속도
    // 이제 인스펙터 창에서 고정 숫자로 입력하지 않으므로 public을 private으로 바꿉니다.
    private float cardOffset;
    private float hideOffsetY;

    private int _maxUnlockedStage;
    private bool _isMoving = false;      // 애니메이션 중 광클 방지

    private RectTransform _activeCard;   // 현재 중앙에 있는 카드
    private RectTransform _hiddenCard;   // 밖에 숨어있는 카드

    public override void Init()
    {
        base.Init();

        // [추가된 핵심 로직] 현재 UI 패널의 '진짜 해상도 크기'를 자동으로 측정합니다.
        RectTransform panelRect = GetComponent<RectTransform>();

        int myClearStage = Managers.PlayerData.PlayerDataLocal.MaxClearStage;
        _maxUnlockedStage = myClearStage + 1;
        // 화면 너비(width)와 높이(height)를 가져와서, 혹시 모르니 안전하게 200픽셀 정도 더 멀리 보냅니다.
        cardOffset = panelRect.rect.width + 200f;
        hideOffsetY = panelRect.rect.height + 200f;

        Bind<Button>(typeof(Buttons));
        Bind<GameObject>(typeof(GameObjects));

        // 1. 이벤트 연결
        GetButton((int)Buttons.Button_GameStart).onClick.AddListener(OnClickGameStart);
        GetButton((int)Buttons.Button_NextStage).onClick.AddListener(OnClickNext);
        GetButton((int)Buttons.Button_PreviousStage).onClick.AddListener(OnClickPrev);


        GameObject card1 = Get<GameObject>((int)GameObjects.Card_1);
        GameObject card2 = Get<GameObject>((int)GameObjects.Card_2);

        _activeCard = card1.GetComponent<RectTransform>();
        _hiddenCard = card2.GetComponent<RectTransform>();

        _activeCard.DOKill();
        _hiddenCard.DOKill();

        // 3. 초기 위치 세팅 (Card_1은 중앙, Card_2는 오른쪽 밖으로)
        _activeCard.anchoredPosition = new Vector2(0, 0);
        _hiddenCard.anchoredPosition = new Vector2(0, hideOffsetY);

        // 4. 첫 화면 데이터 갱신
        _activeCard.GetComponent<UI_StageCard>().SetCard(Managers.Stage.currentStageLevel);
        RefreshUI();
    }
    void OnClickGameStart()
    {
        // 2. [이중 방어 코드] 만약 어떻게든 뚫고 들어왔더라도 여기서 한 번 더 컷!
        if (Managers.Stage.currentStageLevel > _maxUnlockedStage)
        {
            Debug.LogWarning($"[System] 잠긴 스테이지입니다! 현재 최대 진입 가능 스테이지: {_maxUnlockedStage}");
            // (선택) 여기서 "아직 열리지 않은 스테이지입니다" 라는 안내 팝업을 띄우셔도 좋습니다.
            return;
        }
        //  여기서 게임 씬으로 넘어갈 때, Managers나 PlayerPrefs에 _currentStage를 넘겨주면 됩니다.
        // 예: Managers.Game.SelectedStage = _currentStage;
        Managers.Scene.LoadScene(Define.Scene.GameScene);
    }

    void OnClickNext()
    {
        //  참고: 만약 유저가 '미래 스테이지를 구경'하는 것은 허용하고 싶다면 
        // 여기서 _maxUnlockedStage 검사를 빼셔야 합니다!
        // (예: if (_isMoving || Managers.Stage.currentStageLevel >= 100) return;)

        // 애니메이션 중이거나, 마지막 스테이지면 무시
        //if (_isMoving || Managers.Stage.currentStageLevel >= _maxUnlockedStage) return;

        if (_isMoving || Managers.Stage.currentStageLevel >= Managers.Data.StageData.maxStage)
        {
            return;
        }

        Managers.Stage.currentStageLevel++;
        Slide(Vector2.left); // 다음 스테이지니까 카드는 왼쪽으로 밀려야 함
        RefreshUI();
    }

    void OnClickPrev()
    {
        // 애니메이션 중이거나, 1스테이지면 무시
        if (_isMoving || Managers.Stage.currentStageLevel <= 1) return;

        Managers.Stage.currentStageLevel--;
        Slide(Vector2.right); // 이전 스테이지니까 카드는 오른쪽으로 밀려야 함

        RefreshUI();
    }

    private void Slide(Vector2 direction)
    {
        _isMoving = true;

        // 1. 대기 중인 카드(Hidden)를 반대편 밖으로 옮기고 데이터 세팅
        float startX = -direction.x * cardOffset;
        _hiddenCard.anchoredPosition = new Vector2(startX, 0); // Y를 0으로 맞춰서 내려옵니다.
        _hiddenCard.GetComponent<UI_StageCard>().SetCard(Managers.Stage.currentStageLevel);

        // 2. 두 카드를 동시에 이동 (DOTween)
        _activeCard.DOAnchorPos(new Vector2(direction.x * cardOffset, 0), slideDuration).SetEase(Ease.OutCubic);
        _hiddenCard.DOAnchorPos(new Vector2(0,0), slideDuration).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            // 3. 이동 완료 후 활성/비활성 카드 역할 교체
            RectTransform temp = _activeCard;
            _activeCard = _hiddenCard;
            _hiddenCard = temp;

            //4. 이제 밖으로 밀려난(새로운 Hidden) 카드를 다시 '하늘(위쪽 대기실)'로 올려보냄!
            _hiddenCard.anchoredPosition = new Vector2(0, hideOffsetY);

            _isMoving = false;
        });
    }
    private void RefreshUI()
    {
        Button startBtn = GetButton((int)Buttons.Button_GameStart);

        // 현재 보고 있는 스테이지가 내가 입장할 수 있는 최대 스테이지보다 높다면?
        if (Managers.Stage.currentStageLevel > _maxUnlockedStage)
        {
            // 게임 시작 버튼을 완전히 숨깁니다.
            startBtn.gameObject.SetActive(false);

            //  만약 버튼을 숨기는 것보다 회색으로 비활성화하는 게 낫다면 아래 코드를 쓰세요:
            // startBtn.interactable = false;
        }
        else
        {
            // 입장 가능한 스테이지면 버튼을 다시 보여줍니다.
            startBtn.gameObject.SetActive(true);

            //  비활성화 방식을 썼다면:
            // startBtn.interactable = true;
        }
    }

    public override void Clear()
    {
        base.Clear();
    }
}
