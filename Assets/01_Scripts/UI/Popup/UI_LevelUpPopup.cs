using DG.Tweening;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class UI_LevelUpPopup : UI_Popup
{
    enum Panels
    {
        Panel
    }
    private void Start()
    {
        Init();
    }

    // 3개의 카드 붙이기
    public override void Init()
    {
        base.Init();
        Bind<GameObject>(typeof(Panels));

        // 부모 패널 하나만 가져옵니다.
        GameObject cardPanel = GetObject((int)Panels.Panel);
        if (cardPanel == null) return;

        List<AbilityDataSO> abilities = Managers.Ability.GetRandomAbility();
        if (abilities == null) return;

        for (int i = 0; i < abilities.Count; i++)
        {
            GameObject go = Managers.Resource.Instantiate("UI/Popup/UI_AbilityCardButton");
            go.transform.SetParent(cardPanel.transform);
            
            UI_AbilityCard card = Util.GetOrAddComponent<UI_AbilityCard>(go);
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
