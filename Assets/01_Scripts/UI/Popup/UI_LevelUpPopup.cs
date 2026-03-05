using DG.Tweening;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;
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
    public GameObject panel;
    private void Start()
    {
        Init();
    }

    // 3개의 카드 붙이기
    public override void Init()
    {
        base.Init();
        
        Bind<GameObject>(typeof(Cards));

        //// 패널 가져오기
        //GameObject cardPanel = GetObject((int)Panels.Panel);
        if (panel == null) return;

        // 3개의 카드를 담을 배열 생성
        GameObject[] cards = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            // enum 값을 정수로 캐스팅한 뒤 i를 더해서 다음 카드를 가져옴
            cards[i] = GetObject((int)Cards.UI_AbilityCardButton + i);
            // 패널 부모로 붙이기
            cards[i].transform.SetParent(panel.transform);
        }

        // 능력치 가져오기
        List<AbilityDataSO> abilities = Managers.Ability.GetRandomAbility();
        if (abilities == null) return;

        // 능력치랑 카드 갯수 안맞으면 리턴
        if (abilities.Count != cards.Length) return;

        for (int i = 0; i < abilities.Count; i++)
        {
            UI_AbilityCard card = Util.GetOrAddComponent<UI_AbilityCard>(cards[i]);
            card.SetAbilityCard(abilities[i]);

            //RectTransform rect = go.GetComponent<RectTransform>();
            //
            //// ★ 1. 도착할 목표 X 좌표 계산
            //// (0 - 1) * 400 = -400
            //// (1 - 1) * 400 = 0
            //// (2 - 1) * 400 = 400
            //float targetY = (i - (totalCount - 1) / 2f) * spacing;
            //
            //// ★ 2. 시작 위치 설정 (X: -1100)
            //rect.anchoredPosition = new Vector2(-1100f, targetY);
            //
            //// ★ 3. 목표 위치(targetX)로 이동
            //rect.DOAnchorPos(new Vector2(0f, targetY), 0.5f)
            //    .SetEase(Ease.OutBack)
            //    .SetDelay(i * 0.15f)
            //    .SetUpdate(true); // TimeScale 0 무시
        }
    
    }
}
