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

    private bool _isInit = false;

    private LocalizedString _currentNameLoc;
    private LocalizedString _currentDescLoc;

    public UI_MarqueeText descMarquee;
    public override void Init()
    {
        if (_isInit == true)
        {
            return;
        }
        //base.Init();
        // UI 세팅
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));

        _isInit = true;
    }
    public void SetAbilityCard(AbilityDataSO data)
    {
        if (data == null)
            return;

        Init();

        _data = data;
        // 1. 아이콘 세팅
        GetImage((int)Images.AbilityImage).sprite = data.icon;

        ClearLocalization();
        _currentNameLoc = data.localizedName;
        //_currentDescLoc = data.localizedDescription;

        //int nextLevel = data + 1;
        //float nextValue = data.GetValue(nextLevel);
        //_currentDescLoc.Arguments = new object[] { nextValue };

        _currentNameLoc.StringChanged += UpdateNameText;
        _currentDescLoc.StringChanged += UpdateDescText;
    }

    // 번역이 완료되거나 언어가 바뀔 때마다 실행될 콜백 함수들
    private void UpdateNameText(string translatedText)
    {
        GetTMP((int)Texts.AbilityNameText).text = translatedText;

        descMarquee.PlayMarquee(translatedText);
    }

    private void UpdateDescText(string translatedText)
    {
        GetTMP((int)Texts.AbilityDescription).text = translatedText;
    }

    private void ClearLocalization()
    {
        if (_currentNameLoc != null)
        {
            _currentNameLoc.StringChanged -= UpdateNameText;
            _currentNameLoc = null;
        }

        if (_currentDescLoc != null)
        {
            _currentDescLoc.StringChanged -= UpdateDescText;
            _currentDescLoc = null;
        }
    }

    // UI가 화면에서 사라질 때 (팝업이 닫힐 때) 안전하게 메모리 정리
    private void OnDisable()
    {
        ClearLocalization();
    }

}
