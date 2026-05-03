using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using static Define;

public class UI_SettingsPopup : UI_Popup
{
    enum Buttons
    {
        Button_ClosePopup,
        Button_ClosePanelButton, 
        Btn_Previous4, // 이전 언어 (왼쪽 화살표 < )
        Btn_Next4,     // 다음 언어 (오른쪽 화살표 > )
    }
    enum Toggles
    {
        Toggle_SFX,
        Toggle_BGM,
        Toggle_VIBRATE,
    }
    enum Texts
    {
        // hierarchy 구조: LanguagePanel -> Panel_Language -> Text (TMP)에 해당
        // UI_Base 시스템이 이 이름으로 찾을 수 있도록 hierarchy 오브젝트 이름을 맞춰야 합니다.
        // (예: hierarchy의 generic한 'Text (TMP)' 오브젝트 이름을 'Text_LanguageName'으로 변경 후 바인딩)
        Text_LanguageName
    }
    // 언어 순환 선택을 위한 변수들
    private List<Locale> _availableLocales;
    private int _currentLanguageIndex;

    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Bind<Toggle>(typeof(Toggles));

        GetButton((int)Buttons.Button_ClosePopup).onClick.AddListener(()=>ClosePopupUI());
        GetButton((int)Buttons.Button_ClosePanelButton).onClick.AddListener(() => ClosePopupUI());

        // 3. 토글 초기화 및 이벤트 연결
        SetupToggles();

        // 언어 설정
        SetupCyclicLanguageSelector();
    }

    private void SetupToggles()
    {
        // --- BGM 설정 ---
        Toggle toggleBGM = Get<Toggle>((int)Toggles.Toggle_BGM);
        toggleBGM.isOn = Managers.Setting.IsBGMOn; // 현재 값 반영
        toggleBGM.onValueChanged.AddListener((isOn) => {
            Managers.Sound.Play(SoundID.Sfx_UIButtonClick); // 설정 누를 때도 소리나면 좋죠!
            Managers.Setting.ToggleBGM(isOn);
        });

        // --- SFX 설정 ---
        Toggle toggleSFX = Get<Toggle>((int)Toggles.Toggle_SFX);
        toggleSFX.isOn = Managers.Setting.IsSFXOn;
        toggleSFX.onValueChanged.AddListener((isOn) => {
            Managers.Sound.Play(SoundID.Sfx_UIButtonClick);
            Managers.Setting.ToggleSFX(isOn);
        });

        // --- 진동 설정 ---
        Toggle toggleVibrate = Get<Toggle>((int)Toggles.Toggle_VIBRATE);
        toggleVibrate.isOn = Managers.Setting.IsVibrationOn;
        toggleVibrate.onValueChanged.AddListener((isOn) => {
            Managers.Sound.Play(SoundID.Sfx_UIButtonClick);
            Managers.Setting.ToggleVibration(isOn);
        });
    }

    // 새로운 버튼형 순환 언어 선택기 세팅 로직
    private void SetupCyclicLanguageSelector()
    {
        // 1. 유니티 Localization 시스템에서 프로젝트에 등록된 모든 Locale 목록을 가져옵니다.
        _availableLocales = LocalizationSettings.AvailableLocales.Locales;

        if (_availableLocales == null || _availableLocales.Count == 0)
        {
            Debug.LogWarning("[Setting] 사용할 수 있는 Locale 목록이 없습니다.");
            return;
        }

        // 2. 현재 활성화된 언어(코드)를 찾아 초기 인덱스를 설정합니다.
        string currentLangCode = Managers.Setting.Language; // SettingManager에서 가져옴 ("ko", "en" 등)
        _currentLanguageIndex = 0; // 기본값

        for (int i = 0; i < _availableLocales.Count; i++)
        {
            if (_availableLocales[i].Identifier.Code == currentLangCode)
            {
                _currentLanguageIndex = i;
                break;
            }
        }

        // 3. 초기 UI 업데이트 (현재 선택된 언어 이름 표시)
        SetLanguageAtIndex(_currentLanguageIndex, false); // 처음엔 사운드 안 나게 false

        // 4. 화살표 버튼 이벤트 연결
        // delta 가 -1이면 이전, +1이면 다음 언어
        GetButton((int)Buttons.Btn_Previous4).onClick.AddListener(() => {
            OnLanguageSelectorChanged(-1);
        });

        GetButton((int)Buttons.Btn_Next4).onClick.AddListener(() => {
            OnLanguageSelectorChanged(1);
        });
    }

    // delta 값(+1 or -1)에 따라 cyclic하게 index를 계산하고 변경하는 핵심 로직
    private void OnLanguageSelectorChanged(int delta)
    {
        // Managers.Sound.Play(SoundID.Sfx_UIButtonClick); // 버튼 클릭음

        int totalLocales = _availableLocales.Count;
        // cyclic wrapping 로직 (목록 처음/끝 넘어갈 때 순환)
        // 음수 나머지 연산을 안전하게 처리하기 위해 몫을 더해줍니다.
        _currentLanguageIndex = (_currentLanguageIndex + delta + totalLocales) % totalLocales;

        // 계산된 인덱스로 언어 설정 및 UI 갱신
        SetLanguageAtIndex(_currentLanguageIndex);
    }

    // 실제 언어 변경 명령 및 UI Text 갱신 함수
    private void SetLanguageAtIndex(int index, bool playSound = true)
    {
        if (_availableLocales == null || index < 0 || index >= _availableLocales.Count) return;

        Locale selectedLocale = _availableLocales[index];

        // 1. [데이터 갱신] SettingManager를 통해 실제 Locale Code 변경 (예: "ko", "en")
        // 이전 interaction에서 만든 SettingManager.SetLanguage()가 이 코드를 처리합니다.
        // 실제 시스템 언어 변경
        Managers.Setting.SetLanguage(selectedLocale.Identifier.Code);

        // 2. [UI 갱신] 중앙 Text(TMP) 컴포넌트에 현재 Locale 이름을 표시합니다 (예: "ko", "en").
        // localization 된 이름 대신 코드 자체를 보여주거나 
        // selectedLocale.LocaleName (예: "Korean", "English")을 보여줄 수 있습니다.
        // 유니티 Localization 테이블을 이용해 언어 이름 자체도 localize해서 보여주는 것이 상용 수준입니다.

        // [중요 Assumption] hierarchy의 'Text (TMP)' 오브젝트 이름을 'Text_LanguageName'으로 변경해야 binding됨
        //TMP_Text languageNameText = GetTMP((int)Texts.Text_LanguageName);
        //if (languageNameText != null)
        //{
        //    // Locale Identifier 코드 자체를 보여줍니다 ("ko", "en").
        //    // image_2.png처럼 Localized Name (예: "Korean" vs "English")을 보여주려면 
        //    // 별도의 localization String Table을 구성하여 index에 맞춰 text를 변경해야 합니다.
        //    // 일단 identifier code를 보여주도록 작성합니다.
        //    //languageNameText.text = selectedLocale.Identifier.Code.ToUpper();
        //    languageNameText.text = selectedLocale.Identifier.CultureInfo.NativeName;
        //}
    }
}

