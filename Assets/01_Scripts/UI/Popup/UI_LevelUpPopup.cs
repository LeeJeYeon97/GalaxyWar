using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_LevelUpPopup : UI_Popup
{
    enum Panels
    {
        CardPanel,
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

        GameObject panel = GetObject((int)Panels.CardPanel);
        if (panel == null) return;

        List<AbilityDataSO> abilities = Managers.Ability.GetRandomAbility();
        if (abilities == null) return;

        foreach(AbilityDataSO ability in abilities) 
        {
            GameObject go = Managers.Resource.Instantiate("UI/Popup/UI_AbilityCardButton");
            UI_AbilityCard card = Util.GetOrAddComponent<UI_AbilityCard>(go);
            if (card != null)
            {
                card.SetAbilityCard(ability);
                go.transform.SetParent(panel.transform);
            }
        }
    }

}
