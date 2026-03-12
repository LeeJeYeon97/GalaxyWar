using DG.Tweening;
using NUnit.Framework;
using System.Collections.Generic;
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

    private bool _isSelecting = false;
    // 3개의 카드를 담을 배열 생성
    public GameObject[] cards = new GameObject[3];

    private void Start()
    {
        Init();
    }
    // 3개의 카드 붙이기
    public override void Init()
    {
        base.Init();

        _isSelecting = false;
        
        Bind<GameObject>(typeof(Cards));

        // 능력치 가져오기
        List<AbilityDataSO> abilities = Managers.Ability.GetRandomAbility();
        if (abilities == null) return;

        // 능력치랑 카드 갯수 안맞으면 리턴
        if (abilities.Count != cards.Length) return;

        for (int i = 0; i < abilities.Count; i++)
        {

            if (cards[i] == null)
                cards[i] = Get<GameObject>(i);

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
            cardRect.DOAnchorPos(Vector2.zero, 0.5f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)        // 약간 튕기면서 멈추는 찰진 효과
                .SetDelay(i * 0.15f);    // 0번 카드 -> 0.15초 뒤 1번 -> 타다닥 연출!   
        }
    }
    // 2. 카드 클릭 시 실행될 함수 (버튼 OnClick 이벤트에 연결!)
    // 매개변수로 클릭한 카드 자신의 인덱스(0, 1, 2)를 넘겨받아야 합니다.
    public void OnClickCard(int selectedIndex)
    {
        // 1. 이미 다른 카드가 연출 중이면 무시 (더블 클릭 방지)
        if (_isSelecting) return;
        _isSelecting = true;

        Debug.Log($"[{selectedIndex}]번 카드 선택됨! 연출 시작!");

        for (int i = 0; i < cards.Length; i++)
        {
            // UI 객체들의 위치와 투명도를 제어하기 위해 컴포넌트 가져오기
            RectTransform cardRect = cards[i].GetComponent<RectTransform>();
            CanvasGroup cardCanvas = cards[i].GetComponent<CanvasGroup>();

            //필수 확인: 카드 최상위에 'CanvasGroup' 컴포넌트가 붙어 있어야 스르륵(Fade) 사라집니다!
            if (cardCanvas == null) cardCanvas = cards[i].AddComponent<CanvasGroup>();

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
                    Debug.Log("연출 끝! 실제 로직 실행!");
                    // 능력 부여
                    AbilityDataSO data = cards[selectedIndex].GetComponent<UI_AbilityCard>()._data;
                    Managers.Ability.ApplyAbility(data);
                    
                    // 2. UI 닫기 및 게임 재개
                    Managers.Game.ChangeGameState(GameState.Resume);
                    Managers.UI.ClosePopupUI(); // 팝업 끄기
                    
                });
            }
        }
    }
}
