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

        // 방어 로직 추가: upgradeValues가 null이거나 비어있으면 Format을 생략합니다.
        if (upgradeValues == null || upgradeValues.Length == 0)
        {
            // 레벨 0: 수치가 없으므로 번역된 텍스트 원본 그대로 출력
            descText.text = localizedFormat;
        }
        else
        {
            // 레벨 1 이상: 수치가 존재하므로 string.Format으로 {0}, {1} 자리에 끼워 넣음
            descText.text = string.Format(localizedFormat, upgradeValues);
        }
    }
}
