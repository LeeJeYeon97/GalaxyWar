using System;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_GameTestPopup : UI_Popup
{
    // 바인딩할 오브젝트 (스크롤의 Content, 닫기 버튼)
    enum GameObjects
    {
        Content,
        CloseButton
    }
    enum Buttons
    {
        CloseButton
    }

    private void Start()
    {
        Init();
    }

    public override void Init()
    {
        base.Init();
        Bind<GameObject>(typeof(GameObjects));
        Bind<Button>(typeof(Buttons));

        // 1. 닫기 버튼 연결
        Button closeButton = GetButton((int)Buttons.CloseButton);
        closeButton.onClick.AddListener(() => OnClickCloseButton());
        
        

        // 2. 스크롤의 Content 영역 가져오기
        GameObject content = GetObject((int)GameObjects.Content);


        // --- [코드로 레이아웃 설정하기] ---
        // 이미 컴포넌트가 붙어있다면 GetComponent로 가져오고, 없다면 AddComponent 하세요.
        var vLayout = Util.GetOrAddComponent<VerticalLayoutGroup>(content);
        vLayout.childControlWidth = true;  // 가로 사이즈 제어 활성화
        vLayout.childControlHeight = false; // 세로 사이즈는 직접 지정
        vLayout.childForceExpandWidth = true; // 가로 꽉 채우기
        vLayout.childForceExpandHeight = false;
        vLayout.spacing = 10f; // 버튼 사이의 간격
        vLayout.padding = new RectOffset(10, 10, 10, 10); // 안쪽 여백

        var fitter = Util.GetOrAddComponent<ContentSizeFitter>(content);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 3. Enum에 정의된 모든 능력 순회
        foreach (AbilityType type in Enum.GetValues(typeof(AbilityType)))
        {
            // Unknown은 건너뜀
            if (type == AbilityType.Unknown) continue;

            // ★ 해당 Enum에 맞는 SO 데이터 가져오기 (이 함수는 구현이 필요함, 아래 설명 참고)
            
            if (!Managers.Data.AbilityDataDict.ContainsKey(type))
            {
                // 딕셔너리에 키가 없는 경우 방어 코드
                continue;
            }

            AbilityDataSO data = Managers.Data.AbilityDataDict[type];
            if (data == null)
            {
                Debug.LogWarning($"[TestPopup] 데이터가 없습니다: {type}");
                continue;
            }

            // =========================================================
            // ★ 4. 프리팹 없이 코드로 버튼 생성하는 부분 (수정됨)
            // =========================================================

            // (1) 버튼 껍데기(GameObject) 생성
            GameObject btnGO = new GameObject($"Btn_{data.name}");
            btnGO.transform.SetParent(content.transform, false); // false: 로컬 스케일/회전 유지

            // 4. 레이아웃 요소 설정 (버튼의 높이를 고정하기 위해 필수)
            LayoutElement layout = btnGO.AddComponent<LayoutElement>();
            layout.minHeight = 60f; // 버튼의 최소 세로 높이 지정
            layout.preferredHeight = 60f;

            
            // (3) Image 컴포넌트 추가 (버튼 배경)
            Image btnImage = btnGO.AddComponent<Image>();
            btnImage.color = new Color(1f, 1f, 1f, 0.8f); // 흰색 배경 (약간 투명)

            // (4) Button 컴포넌트 추가
            Button btn = btnGO.AddComponent<Button>();

            // (5) 텍스트를 담을 자식 오브젝트 생성
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);

            // (6) 텍스트 RectTransform 설정 (버튼에 꽉 차게)
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // (7) Text 컴포넌트 추가 및 설정
            Text txt = textGO.AddComponent<Text>();
            txt.text = data.name; // 데이터 이름 넣기
            txt.color = Color.black;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.resizeTextForBestFit = true; // 텍스트 크기 자동 조절
            txt.resizeTextMinSize = 10;
            txt.resizeTextMaxSize = 30;

            // ★ 중요: 코드로 텍스트 만들 때 폰트가 없으면 안 보입니다.
            // 기본 Arial 폰트를 가져오거나 Resources.GetBuiltinResource를 사용합니다.
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // =========================================================
            // ★ 클릭 이벤트 연결
            // =========================================================
            btn.onClick.AddListener(() => OnClickTestAbility(data));
        }
    }

    // 테스트용: 클릭하면 능력 즉시 획득
    private void OnClickTestAbility(AbilityDataSO data)
    {
        Debug.Log($"[Test] 능력 획득: {data.name}");


        // 능력 부여
        Managers.Ability.ApplyAbility(data);

        // 2. UI 닫기 및 게임 재개
        Managers.UI.ClosePopupUI();
        Managers.Game.ChangeGameState(GameState.Resume);
        
    }
    private void OnClickCloseButton()
    {

        Debug.Log($"Test Popup Close Button Click");
        Managers.UI.ClosePopupUI();
        Managers.Game.ChangeGameState(GameState.Resume);
    }
}
