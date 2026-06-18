using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_UpgradeNode : UI_Base
{
    enum Images
    {
        Image_Icon,
        Image_Line,
        Image_LineBG

    }
    enum Texts
    {
        Text_Level
    }

    // 이 노드가 담당하는 특정 레벨과, 바로 앞 노드
    private int _targetLevel;
    private UI_UpgradeNode _prevNode;

    private UpgradeDataSO _myData;

    // 외부에서 이 노드가 구매 완료되었는지 확인할 수 있는 프로퍼티
    public bool IsPurchased { get; private set; }

    public override void Init()
    {
        base.Init();
        Bind<Image>(typeof(Images));
        Bind<TMP_Text>(typeof(Texts));

        //  1. 유니티 기본 Button 컴포넌트를 가져와서 클릭 이벤트 연결
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClickNode);
        }
        else
        {
            Debug.LogWarning("UI_UpgradeNode 프리팹에 Button 컴포넌트가 없습니다!");
        }
    }

    public void SetInfo(UpgradeDataSO data, int targetLevel, UI_UpgradeNode prevNode)
    {
        if (!_init) Init();
        _myData = data;
        _targetLevel = targetLevel;
        _prevNode = prevNode;

        RefreshUI();
    }

    public void RefreshUI()
    {
        //  2. 혼재되어 있던 로직 정리 (_prevData 대신 _targetLevel과 _prevNode 사용)
        int myCurrentLevel = Managers.Upgrade.GetUpgradeLevel(_myData.type);

        // 내 현재 레벨이 목표 레벨에 도달했거나 넘었다면 구매 완료된 것!
        IsPurchased = myCurrentLevel >= _targetLevel;

        // 내 바로 앞 노드가 있고, 그 앞 노드가 아직 구매되지 않았다면 나는 "잠금" 상태
        bool isLocked = false;
        if (_prevNode != null && !_prevNode.IsPurchased)
        {
            isLocked = true;
        }

        // 아이콘 세팅
        GetImage((int)Images.Image_Icon).sprite = _myData.iconSprite;
        GetImage((int)Images.Image_Icon).color = isLocked ? Color.gray : Color.white;

        bool isLastNode = _targetLevel >= _myData.MaxLevel;
        Image lineImg = GetImage((int)Images.Image_Line);

        // 만약 마지막 노드라면 선을 아예 끕니다.
        if (isLastNode)
        {
            lineImg.gameObject.SetActive(false);
            GetImage((int)Images.Image_LineBG).gameObject.SetActive(false);
        }
        else
        {
            // 마지막 노드가 아닐 때만 기존 로직 수행
            lineImg.gameObject.SetActive(true);
            lineImg.fillAmount = IsPurchased ? 1f : 0f;
        }

        // 텍스트 세팅 (목표 레벨 표시, 이미 샀으면 완료 표시)
        //GetTMP((int)Texts.Text_Level).text = IsPurchased ? "완료" : $"Lv.{_targetLevel}";

        // 선 연결 로직 (내가 구매 완료되었다면 선 활성화)
        //GetImage((int)Images.Image_Line).gameObject.SetActive(IsPurchased);

        // [변경점] 이미 구매된 노드라면 패널을 열었을 때 선이 처음부터 꽉 차있게 세팅
        //Image lineImg = GetImage((int)Images.Image_Line);
        //lineImg.fillAmount = IsPurchased ? 1f : 0f;
    }

    public void OnClickNode()
    {
        Managers.Sound.Play(Define.SoundID.Sfx_UIButtonClick);
        //  3. 클릭 시 예외 처리 추가 (잠겨있거나 이미 샀으면 팝업 안 띄움)
        if (_prevNode != null && !_prevNode.IsPurchased)
        {
            Debug.Log("이전 단계를 먼저 해금해야 합니다!");
            return;
        }

        if (IsPurchased)
        {
            Debug.Log("이미 달성한 능력입니다!");
            return;
        }

        // 4. 팝업 띄우기 (주석 해제)
        var popup = Managers.UI.ShowPopupUI<UI_UpgradePopup>();
        popup.SetInfo(_myData, this);
    }
    public void PlayUnlockAnimation()
    {
        IsPurchased = true;

        //  [방어 코드] 마지막 노드라면 선 애니메이션을 생략합니다.
        bool isLastNode = _targetLevel >= _myData.MaxLevel;
        if (isLastNode)
        {
            // 선이 없으므로 바로 패널만 갱신하고 끝냅니다.
            UI_UpgradePanel panel = FindAnyObjectByType<UI_UpgradePanel>();
            if (panel != null) panel.UpdateNodesUI();

            //  [옵션 3] 만렙 달성 전용 빰빠라밤~ 사운드
            // Managers.Sound.Play("Sounds/UI/MaxLevelSuccess");
            return;
        }

        Image lineImg = GetImage((int)Images.Image_Line);
        lineImg.gameObject.SetActive(true);
        lineImg.fillAmount = 0f; // 0에서 시작

        // =======================================================
        //  [옵션 1] 선이 "차오르기 시작할 때" 재생 (예: 위이잉~ 차오르는 소리)
        // =======================================================
        Managers.Sound.Play(Define.SoundID.Sfx_UpgradeLineUpSound); 

        // 0.5초 동안 선이 위로 쭈욱 차오르는 DOTween 애니메이션
        lineImg.DOFillAmount(1f, 0.5f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            // =======================================================
            // [옵션 2] 선이 다 차고 "다음 노드가 켜질 때" 재생 (예: 챙! 하는 경쾌한 소리)
            // =======================================================
            Managers.Sound.Play(Define.SoundID.Sfx_UpgradeIconOpenSound);

            // 선이 끝까지 닿으면 팅! 하고 다음 노드의 색상을 밝혀줍니다.
            UI_UpgradePanel panel = FindAnyObjectByType<UI_UpgradePanel>();
            if (panel != null)
            {
                panel.UpdateNodesUI();
            }
        });
    }
}