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
        AbilityLevel,
    }
    public AbilityDataSO _data;

    public TMP_Text nameText;
    public TMP_Text descText;
    public TMP_Text levelText;

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
        levelText = GetTMP((int)Texts.AbilityLevel);
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


        // 2. 현재 레벨 및 다음 레벨 계산
        int currentLevel = Managers.Ability.GetCurrentLevel(_data.type);
        int nextLevel = currentLevel + 1; // 다음 레벨

        //  2. 이름 뒤에 레벨업 텍스트를 붙여서 출력합니다.
        string nameKey = $"{data.type}_Name";
        string localizedName = Util.GetLocalizeString("Ability", nameKey);

        // 완성된 이름 문자열을 담을 변수
        string finalNameText = $"{localizedName}";

        //if (nextLevel >= Managers.Ability.GetMaxLevel(_data.type))
        //{
        //    finalNameText = $"{localizedName} <size=80%>(Lv. {currentLevel} -> MAX)</size>";
        //}
        //else
        //{
        //    finalNameText = $"{localizedName} <size=80%>(Lv. {currentLevel} -> Lv. {nextLevel})</size>";
        //}

        // 4. 마키(Marquee) 컴포넌트를 가져와서 완성된 텍스트를 넘겨줍니다!
        UI_MarqueeText nameMarquee = nameText.GetComponent<UI_MarqueeText>();
        if (nameMarquee != null)
        {
            nameMarquee.PlayMarquee(finalNameText);
        }
        else
        {
            // 마키 스크립트가 없다면 그냥 텍스트만 띄움 (방어 코드)
            nameText.text = finalNameText;
        }

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

        if(data.type == AbilityType.Passive_PlayerHeal)
        {
            levelText.gameObject.SetActive(false);
        }
        else if (nextLevel >= Managers.Ability.GetMaxLevel(_data.type))
        {
            levelText.text = $"Lv. {currentLevel} -> Lv. MAX";
            levelText.gameObject.SetActive(true);
        }
        else
        {
            levelText.text = $"Lv. {currentLevel} -> Lv. {nextLevel}";
            levelText.gameObject.SetActive(true);
        }
    }
}
