using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using static Define;

public class UI_AbilityCard : UI_Base
{
    // 데이터 정보 넣기

    enum Images
    {
        AbilityImage,
    }
    enum Texts
    {
        AbilityNameText,
        AbilityDescription,
    }
    public AbilityDataSO _data;

    public TMP_Text nameText;
    public TMP_Text descText;

    public UI_MarqueeText descMarquee;
    public override void Init()
    {
        if (_init)
        {
            return;
        }

        base.Init();
        Bind<Image>(typeof(Images));
        Bind<TMP_Text>(typeof(Texts));

        nameText = GetTMP((int)Texts.AbilityNameText);
        descText = GetTMP((int)Texts.AbilityDescription);

    }
    public void SetAbilityCard(AbilityDataSO data)
    {
        if (data == null)
            return;

        if(_init == false)
        {
            Init();
        }

        _data = data;

        // 1. 아이콘 세팅
        GetImage((int)Images.AbilityImage).sprite = data.icon;


        // 1. 플레이어가 현재 이 능력을 몇 레벨 가지고 있는지 확인합니다.
        // 현재 레벨 처음이면 0
        int currentLevel = Managers.Ability.GetCurrentLevel(_data.type);

        string nameKey = $"{data.type}_Name";
        nameText.text = Util.GetLocalizeString("Ability", nameKey);

        
        string descKey = $"{data.type}_Desc_{currentLevel}";

        // 0레벨이면 "범위 공격을 주는 폭발탄을 획득합니다."가 그대로 출력됨
        string localizedFormat = Util.GetLocalizeString("Ability", descKey);

        object[] upgradeValues = _data.GetUpgradeValues();

        // 4. 조립 및 출력
        // 레벨 0 텍스트에 {0}이 없으면? -> 알아서 무시하고 "범위 공격을 획득합니다." 출력
        // 레벨 1 텍스트에 {0}이 있으면? -> 알아서 수치 넣고 "범위가 10% 증가합니다." 출력
        descText.text = string.Format(localizedFormat, upgradeValues);

    }
}
