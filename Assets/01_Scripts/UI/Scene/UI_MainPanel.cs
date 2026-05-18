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

    private int _maxUnlockedStage = 100; // 테스트용: 나중에 실제 저장 데이터로 교체
    private bool _isMoving = false;      // 애니메이션 중 광클 방지

    private RectTransform _activeCard;   // 현재 중앙에 있는 카드
    private RectTransform _hiddenCard;   // 밖에 숨어있는 카드

    public override void Init()
    {
        base.Init();

        // [추가된 핵심 로직] 현재 UI 패널의 '진짜 해상도 크기'를 자동으로 측정합니다.
        RectTransform panelRect = GetComponent<RectTransform>();

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
    }
    void OnClickGameStart()
    {
        //  여기서 게임 씬으로 넘어갈 때, Managers나 PlayerPrefs에 _currentStage를 넘겨주면 됩니다.
        // 예: Managers.Game.SelectedStage = _currentStage;
        Managers.Scene.LoadScene(Define.Scene.GameScene);
    }

    void OnClickNext()
    {
        // 애니메이션 중이거나, 마지막 스테이지면 무시
        if (_isMoving || Managers.Stage.currentStageLevel >= _maxUnlockedStage) return;

        Managers.Stage.currentStageLevel++;
        Slide(Vector2.left); // 다음 스테이지니까 카드는 왼쪽으로 밀려야 함
    }

    void OnClickPrev()
    {
        // 애니메이션 중이거나, 1스테이지면 무시
        if (_isMoving || Managers.Stage.currentStageLevel <= 1) return;

        Managers.Stage.currentStageLevel--;
        Slide(Vector2.right); // 이전 스테이지니까 카드는 오른쪽으로 밀려야 함
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

    public override void Clear()
    {
        base.Clear();
    }
}
