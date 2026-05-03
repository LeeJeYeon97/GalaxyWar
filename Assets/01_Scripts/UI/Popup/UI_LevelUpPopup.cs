using DG.Tweening;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Define;
using static UnityEngine.GraphicsBuffer;

public class UI_LevelUpPopup : UI_Popup
{
    enum Panels
    {
        Panel
    }
    enum Cards
    {
        UI_AbilityCardButton,
        UI_AbilityCardButton_1,
        UI_AbilityCardButton_2
    }
    enum Buttons
    {
        ReloadButton_AD,
        ReloadButton_Coin
    }
    private bool _isSelecting = false;
    // 3개의 카드를 담을 배열 생성
    public GameObject[] cards = new GameObject[3];

    // 3개의 카드 붙이기
    public override void Init()
    {
        base.Init();

        _isSelecting = false;
        
        Bind<GameObject>(typeof(Cards));
        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.ReloadButton_AD).onClick.AddListener(OnCardReloadButtonAd);

        GetButton((int)Buttons.ReloadButton_Coin).onClick.AddListener(OnCardReloadButtonCoinAsync);

        RefreshCards();
    }
    private void OnCardReloadButtonAd()
    {
        if (_isSelecting) return;
        _isSelecting = true;

        // 광고보게하기
        // 플레이스먼트
        // 광고 보기 (두 번째 파라미터로 콜백 함수를 화살표 함수 형태로 넘깁니다)
        Managers.AD.ShowRewardedAd(placement_InGameCardReload, (success) =>
        {
            if (success)
            {
                Debug.Log("광고 시청 완료! 카드를 리롤합니다.");
                RefreshCards();
            }
            else
            {
                Debug.Log("광고 시청에 실패했거나 취소했습니다.");
                // 필요하다면 유저에게 "광고 시청 실패" 안내 메시지 띄우기
            }
        });
    }
    private async void OnCardReloadButtonCoinAsync()
    {
        // 
        // 1. 연속 클릭 방지 및 로딩 표시
        if (_isSelecting) return;
        _isSelecting = true;

        // 유저에게 "처리 중..."임을 알리기 위해 로딩 팝업을 띄웁니다.
        Managers.UI.ShowPopupUI<UI_LoadingPopup>();

        try
        {
            // 2. 서버(Cloud Code)에 코인 소모 요청
            // (서버 함수 이름이 'SpendCurrency'이고, 인자로 재화 ID와 소모량을 보낸다고 가정)
            // 성공 시 업데이트된 경제 데이터(Currency 등)를 반환받습니다.
            var spendCurrency = await Managers.PlayerEconomy.SpendGoldAsync(100);

            if (spendCurrency == true)
            {
                Debug.Log("코인 소모 성공! 카드를 리롤합니다.");

                // 로딩 팝업 닫고 카드 리프레시
                Managers.UI.ClosePopupUI();
                RefreshCards();
            }
            else
            {
                // 서버 결과가 실패(코인 부족 등)인 경우
                HandleCoinReloadFailed("코인이 부족합니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"코인 리롤 중 서버 에러 발생: {e.Message}");
            HandleCoinReloadFailed("네트워크 통신에 실패했습니다.");
        }
    }
    private void HandleCoinReloadFailed(string message)
    {
        Managers.UI.ClosePopupUI();
        _isSelecting = false;

        if(Managers.Game.currentGameState == GameState.Pause)
        {
            Managers.Game.ChangeGameState(GameState.Resume);
        }

        // 여기에 유저에게 보여줄 알림 팝업(예: UI_Toast)을 추가하면 더 좋습니다.
        Debug.LogWarning(message);
    }

    private void RefreshCards()
    {
        // 1. 안전장치: 카드가 세팅되고 날아오는 동안에는 절대 클릭 못하게 잠급니다!
        _isSelecting = true;

        // 능력치 가져오기
        List<AbilityDataSO> abilities = Managers.Ability.GetRandomAbility();

        if (abilities == null || abilities.Count != cards.Length)
        {
            return;
        }

        for (int i = 0; i < abilities.Count; i++)
        {
            if (cards[i] == null)
            {
                cards[i] = Get<GameObject>(i);
            }
            // 카드 UI 세팅
            UI_AbilityCard card = Util.GetOrAddComponent<UI_AbilityCard>(cards[i]);
            
            card.SetAbilityCard(abilities[i]);

            int capturedIndex = i;

            // 버튼 연결
            Button cardButton = card.GetComponent<Button>();
            cardButton.onClick.RemoveAllListeners();
            cardButton.interactable = true;
            cardButton.onClick.AddListener(() => OnClickCard(capturedIndex));

            // 핵심: 배열에 들어있는 게임 오브젝트에서 RectTransform을 꺼내옵니다.
            RectTransform cardRect = card.GetComponent<RectTransform>();

            // 혹시라도 UI 객체가 아닌 게 들어왔을 때 에러가 나지 않도록 안전장치를 걸어줍니다.
            if (cardRect == null) continue;

            // 1. 애니메이션 시작 전: 카드를 껍데기(Slot) 기준 왼쪽 밖(-1500)으로 치워둡니다.
            cardRect.anchoredPosition = new Vector2(-1500f, 0f);

            // 1. 크기와 투명도 원래대로 복구
            cardRect.localScale = Vector3.one;

            CanvasGroup cardCanvas = Util.GetOrAddComponent<CanvasGroup>(cards[i]);
            cardCanvas.alpha = 1f; // 투명도 100%로 복구

            // 2. DOTween 애니메이션: 오른쪽 밖에서 원래 자리(0,0)로 날아오기!                      
            var moveTween = cardRect.DOAnchorPos(Vector2.zero, 0.5f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)        // 약간 튕기면서 멈추는 찰진 효과
            .SetDelay(i * 0.15f);   // 0번 카드 -> 0.15초 뒤 1번 -> 타다닥 연출!

            // ★ 3. 핵심 방어 로직: 마지막 카드(3번째)가 도착했을 때 비로소 잠금을 풉니다!
            if (i == abilities.Count - 1)
            {
                moveTween.OnComplete(() =>
                {
                    _isSelecting = false; // 이제 마음껏 고르세요!
                    Debug.Log("모든 카드 도착 완료! 선택 가능 상태로 전환.");
                });
            }
        }
    }
    // 2. 카드 클릭 시 실행될 함수 (버튼 OnClick 이벤트에 연결!)
    // 매개변수로 클릭한 카드 자신의 인덱스(0, 1, 2)를 넘겨받아야 합니다.
    public void OnClickCard(int selectedIndex)
    {
        // 1. 이미 다른 카드가 연출 중이면 무시 (더블 클릭 방지)
        if (_isSelecting) return;
        _isSelecting = true;

        for (int i = 0; i < cards.Length; i++)
        {
            // UI 객체들의 위치와 투명도를 제어하기 위해 컴포넌트 가져오기
            RectTransform cardRect = cards[i].GetComponent<RectTransform>();
            CanvasGroup cardCanvas = cards[i].GetComponent<CanvasGroup>();

            //필수 확인: 카드 최상위에 'CanvasGroup' 컴포넌트가 붙어 있어야 스르륵(Fade) 사라집니다!
            if (cardCanvas == null) 
                cardCanvas = cards[i].AddComponent<CanvasGroup>();

            //  3. 선택받지 못한 다른 카드들 연출
            if (i != selectedIndex)
            {
                // 즉시 반응: 0.2초 동안 투명해지면서 스르륵 사라짐
                cardCanvas.DOFade(0f, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true);

                // (선택) 살짝 아래로 가라앉으면서 사라지면 더 예쁩니다.
                cardRect.DOAnchorPosY(-100f, 0.2f).SetEase(Ease.OutCubic).SetUpdate(true);

                // 버튼 기능도 즉시 끕니다.
                cards[i].GetComponent<Button>().interactable = false;
            }
            //  4. 선택받은 카드 연출 (시퀀스 콤보!)
            else
            {
                // 버튼 기능 즉시 끄기
                cards[i].GetComponent<Button>().interactable = false;

                // 시퀀스 생성 (타임라인)
                DG.Tweening.Sequence seq = DOTween.Sequence();
                seq.SetUpdate(true);
                // 콤보 1: 밝게 빛나면서 통! 튀기 (크기 1.0 -> 1.2로 커지기)
                // ( 밝게 빛나는 건 쉐이더가 필요하므로, 여기서는 크기 피드백으로 대체합니다.)
                seq.Append(cardRect.DOScale(1.2f, 0.15f).SetEase(Ease.OutCubic));
                
                // 콤보 2: 0.1초 잠깐 대기 (강조 효과)
                seq.AppendInterval(0.1f);

                // 콤보 3: 순식간에 0으로 작아지면서 사라지기! (동시에 일어나게 Join 사용)
                seq.Append(cardRect.DOScale(0f, 0.2f).SetEase(Ease.InBack)); // 통! 하고 박살 나는 느낌
                seq.Join(cardCanvas.DOFade(0f, 0.15f)); // 투명해지기

                // 콤보 4: 애니메이션이 모두 끝난 뒤에 할 일 (부활, 팝업 끄기 등)
                seq.OnComplete(() =>
                {
                    // 능력 부여
                    AbilityDataSO data = cards[selectedIndex].GetComponent<UI_AbilityCard>()._data;
                    if(data == null)
                    {
                        Debug.LogError("Data null");
                    }
                    
                    Managers.Ability.ApplyAbility(data);

                    // ★ 2. 레벨업 횟수 차감 및 재확인 로직
                    // (Managers.Game.PendingLevelUpCount에 접근한다고 가정)
                    Managers.Level.PendingLevelUpCount--;

                    if (Managers.Level.PendingLevelUpCount > 0)
                    {
                        // 횟수가 남았다면 팝업을 끄지 않고 카드만 리필!
                        RefreshCards();
                    }
                    else
                    {
                        // 모두 끝났다면 게임 재개 및 팝업 닫기
                        Managers.Game.ChangeGameState(GameState.Resume);
                        Managers.UI.ClosePopupUI();
                    }
                });
            }
        }
    }
}
