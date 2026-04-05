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


    private LocalizedString _currentNameLoc;
    private LocalizedString _currentDescLoc;

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

        int nextLevel = Managers.Ability.GetCurrentLevel(data.type) + 1;
        // ★ 다형성의 마법: 이게 화염탄이든 체력증가든 알아서 자기 레벨에 맞는 설명을 뱉어냅니다!
        if (nextLevel > 0 && nextLevel <= data.localizationDesc.Count)
        {
            _currentDescLoc = data.localizationDesc[nextLevel - 1];

            // ★ 2. 마법의 코드: 번역 텍스트 안의 {0.damage} 같은 구멍을 실제 데이터로 채워줍니다!
            //object levelData = data.(nextLevel);
            //if (levelData != null)
            //{
            //    _currentDescLoc.Arguments = new object[] { levelData };
            //}
        }

        // [참고] 스마트 스트링(Arguments)이 필요하다면 아래처럼 쓰시면 되지만,
        // 이제 레벨마다 LocalizedString이 따로 있으므로, 로컬라이제이션 테이블(번역표)에 
        // "데미지 5 증가" 라고 직접 적어두는 것이 훨씬 관리가 편합니다!
        // _currentDescLoc.Arguments = new object[] { nextValue }; // (이 부분은 삭제 추천!)

        // 4. 이벤트 등록
        if (_currentNameLoc != null)
            _currentNameLoc.StringChanged += UpdateNameText;

        if (_currentDescLoc != null)
            _currentDescLoc.StringChanged += UpdateDescText;
    }

    // 번역이 완료되거나 언어가 바뀔 때마다 실행될 콜백 함수들
    private void UpdateNameText(string translatedText)
    {
        TMP_Text name = GetTMP((int)Texts.AbilityNameText);
        name.text = translatedText;

        descMarquee = name.GetComponent<UI_MarqueeText>();
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
