using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
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
    private Button myButton;

    
    public AbilityDataSO _data;

    public void Start()
    {
        Init();
    }
    public override void Init()
    {
        //base.Init();
    }
    public void SetAbilityCard(AbilityDataSO data)
    {
        if (data == null)
            return;

        _data = data;

        // UI 세팅
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));

        // 데이터에 따른 이미지 및 스킬 설명 세팅
        GetTMP((int)Texts.AbilityNameText).text = _data.abilityname;

        int currentLevel = Managers.Ability.GetCurrentLevel(data.type);
        int targetLevel = Mathf.Clamp(currentLevel, 0, data.maxLevel - 1);

        float value = data.values[targetLevel];

        string displayValue = "";
        // 1.0보다 작으면 확률(0.1, 0.2...)로 판단하여 %로 변환
        if (value > 0 && value < 1.0f)
        {
            // 0.1 -> "10"
            displayValue = (value * 100f).ToString("N0");
        }
        else
        {
            displayValue = value.ToString("N0");
        }
        GetTMP((int)Texts.AbilityDescription).text = string.Format(data.description, displayValue);

        GetImage((int)Images.AbilityImage).sprite = _data.icon;

    }

}
