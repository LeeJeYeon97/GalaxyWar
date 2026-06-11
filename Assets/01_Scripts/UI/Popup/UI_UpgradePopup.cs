using TMPro;
using UnityEngine;
using UnityEngine.UI; // Button 컴포넌트를 사용하기 위해 필수입니다.

public class UI_UpgradePopup : UI_Popup
{
    enum Texts { Text_Name, Text_Desc, Text_Cost, Text_StatDiff }
    enum Buttons { Button_Upgrade, Button_Close, Button_ClosePopup }

    private UpgradeDataSO _data;
    private UI_UpgradeNode _ownerNode;

    public override void Init()
    {
        base.Init();
        Bind<TMP_Text>(typeof(Texts));
        Bind<Button>(typeof(Buttons)); //  GameObject가 아니라 Button으로 바인딩합니다.

        //  BindEvent 대신 유니티 기본 리스너 사용
        GetButton((int)Buttons.Button_Upgrade).onClick.AddListener(OnClickUpgrade);
        GetButton((int)Buttons.Button_Close).onClick.AddListener(() => Managers.UI.ClosePopupUI(this));
        GetButton((int)Buttons.Button_ClosePopup).onClick.AddListener(() => ClosePopupUI());
    }

    public void SetInfo(UpgradeDataSO data, UI_UpgradeNode node)
    {
        if (!_init) Init();
        _data = data;
        _ownerNode = node;

        RefreshUI();

        // 뿅! 애니메이션은 인스펙터 DOTween (OnEnable)으로 처리된다고 가정
    }

    void RefreshUI()
    {
        // DataManager 대신 UpgradeManager에서 현재 레벨을 가져옵니다. (upgradeID -> type)
        int curLevel = Managers.Upgrade.GetUpgradeLevel(_data.type);
        bool isMax = curLevel >= _data.MaxLevel;

        GetTMP((int)Texts.Text_Name).text = _data.upgradeName;

        if (isMax)
        {
            GetTMP((int)Texts.Text_Desc).text = "최대 레벨에 도달했습니다.";
            GetTMP((int)Texts.Text_Cost).text = "MAX";
            GetButton((int)Buttons.Button_Upgrade).interactable = false;
        }
        else
        {
            // 데이터 구조에서 "다음 레벨" 정보 가져오기
            var nextInfo = _data.levelInfos[curLevel];
            GetTMP((int)Texts.Text_Desc).text = nextInfo.description;
            GetTMP((int)Texts.Text_Cost).text = $"x{nextInfo.cost.ToString()}";

            // 수치 변화 보여주기 (예: HP 150 > 200)
            //float curVal = (curLevel > 0) ? _data.levelInfos[curLevel - 1].statValue : 0;
            //GetTMP((int)Texts.Text_StatDiff).text = $"{curVal} > {nextInfo.statValue}";

            // 실제 게임의 재화(Gold 등) 변수에 맞게 주석을 풀고 사용하세요!
            GetButton((int)Buttons.Button_Upgrade).interactable = (Managers.PlayerEconomy.Gold >= nextInfo.cost);
            //GetButton((int)Buttons.Button_Upgrade).interactable = true; // 임시로 무조건 클릭 가능하게 활성화
        }
    }

    private async void OnClickUpgrade()
    {
        GetButton((int)Buttons.Button_Upgrade).interactable = false;

        //  함수 이름이 바뀌었고, 서버 통신이 끝날 때까지 await로 기다립니다!
        bool isSuccess = await Managers.Upgrade.TryLevelUpAsync(_data);

        if (isSuccess)
        {
            Managers.UI.ClosePopupUI(this);

            if (_ownerNode != null)
            {
                _ownerNode.PlayUnlockAnimation();
            }
        }
        else
        {
            Debug.Log("재화가 부족하거나 통신에 실패했습니다.");
        }
    }
}