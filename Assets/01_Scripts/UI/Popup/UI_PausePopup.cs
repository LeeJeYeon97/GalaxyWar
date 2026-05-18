using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_PausePopup : UI_Popup
{
    enum Buttons
    {
        Btn_Resume,
        Btn_ReStart,
        Btn_Settings,
        Btn_QuitGame
    }
    enum Texts
    {
        Text_Damage,
        Text_CriticalChance,
        Text_CriticalDamage,
        Text_MaxHp,
        Text_Speed,
        Text_ReloadCount,
        Text_ReloadTime,
        Text_ShotTime,
        Text_BurstChargeTime,
        Text_ShieldChargeTime,
        Text_SplitCount,
        Text_BounceCount,
    }
    // 1. Content 객체를 찾기 위한 Enum 추가
    enum GameObjects
    {
        Content_AbilityList // 가로 스크롤 뷰의 Content 오브젝트 이름
    }

    public GameObject abilityItemPrefab;

    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects)); // Content 바인딩

        GetButton((int)Buttons.Btn_Resume).onClick.AddListener(OnClickResumeButton);
        GetButton((int)Buttons.Btn_ReStart).onClick.AddListener(OnClickRestartButton);
        GetButton((int)Buttons.Btn_Settings).onClick.AddListener(OnClickSettingButton);
        GetButton((int)Buttons.Btn_QuitGame).onClick.AddListener(OnClickQuitGameButton);

        RefreshStatText();
        RefreshAbilityList();
    }
    private void RefreshStatText()
    {
        // 현재 플레이어 스텟 텍스트 세팅
        PlayerStat stat = Managers.Stat.playerStat;

        // 공격력
        GetTMP((int)Texts.Text_Damage).text = Managers.Stat.bulletStatDict[BulletType.NormalBullet].damage.TotalValue.ToString();

        // 치명타 확률 (확률이므로 % 붙이기)
        GetTMP((int)Texts.Text_CriticalChance).text = $"{stat.criticalChance.TotalValue}%";

        // 치명타 데미지 (비율이므로 % 또는 배수로 표현)
        GetTMP((int)Texts.Text_CriticalDamage).text = $"{stat.criticalDamageRate.TotalValue}%";

        // 최대 체력
        GetTMP((int)Texts.Text_MaxHp).text = stat.maxHp.TotalValue.ToString();

        // 이동속도
        GetTMP((int)Texts.Text_Speed).text = stat.speed.TotalValue.ToString();

        // 재장전 갯수
        GetTMP((int)Texts.Text_ReloadCount).text = stat.reloadCount.TotalValue.ToString();

        // 재장전 시간 (소수점 1자리 + 초)
        GetTMP((int)Texts.Text_ReloadTime).text = $"{stat.reloadTime.TotalValue:F1}초";

        // 샷 시간 (소수점 2자리 + 초, 예: 0.15초)
        GetTMP((int)Texts.Text_ShotTime).text = $"{stat.shotTime.TotalValue:F2}초";

        // 버스트 모드 충전 시간
        GetTMP((int)Texts.Text_BurstChargeTime).text = $"{stat.maxBurstFullChargeTime.TotalValue:F1}초";

        // 쉴드 충전 시간
        GetTMP((int)Texts.Text_ShieldChargeTime).text = $"{stat.shieldChargeTime.TotalValue:F1}초";

        // 분열탄 갯수
        GetTMP((int)Texts.Text_SplitCount).text = stat.multiShotCount.TotalValue.ToString();

        // 탄 튕김 횟수
        GetTMP((int)Texts.Text_BounceCount).text = Managers.Stat.bulletStatDict[BulletType.NormalBullet].bounceCount.TotalValue.ToString();
    }
    private void RefreshAbilityList()
    {
        // 1. Content 오브젝트 가져오기
        GameObject content = GetObject((int)GameObjects.Content_AbilityList);
        if (content == null) return;

        // 2. 이전에 켜졌을 때 만들어둔 옛날 아이콘들 싹 지우기 (초기화)
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);
        }

        // 3. 현재 유저가 게임(이번 판)에서 획득한 스킬 리스트 가져오기
        // (주의: 이 부분은 대표님의 실제 데이터 구조에 맞게 수정하셔야 합니다!)
        Dictionary<AbilityType, int> myAbilities = Managers.Ability._abilityLevels;
        
        // 4. 보유한 스킬 개수만큼 프리팹을 생성하고 데이터(사진, 레벨) 세팅
        foreach (var ability in myAbilities)
        {
            AbilityType skillId = ability.Key;
            int skillLevel = ability.Value;

            // 프리팹 생성 (부모를 Content로 지정)
            GameObject itemGo = Managers.Resource.Instantiate(abilityItemPrefab, content.transform);
            UI_AbilityListItem itemUI = itemGo.GetComponent<UI_AbilityListItem>();

            // 스킬 ID를 바탕으로 DataManager 등에서 실제 아이콘 스프라이트를 찾아옵니다.
            // 예시: Sprite icon = Managers.Data.SkillDataDict[skillId].icon;
            Sprite dummyIcon = Managers.Data.AbilityDataDict[skillId].icon; 

            // UI 업데이트
            if (itemUI != null)
            {
                itemUI.SetInfo(dummyIcon, skillLevel);
            }
        }
    }

    private void OnClickResumeButton()
    {
        ClosePopupUI();
        Managers.Game.ChangeGameState(GameState.Resume);
    }
    private void OnClickRestartButton()
    {
        // 현재 씬(GameScene)을 다시 로드! (가장 깔끔한 초기화)
        Managers.Scene.LoadScene(Define.Scene.GameScene);
    }
    private void OnClickSettingButton()
    {
        Managers.UI.ShowPopupUI<UI_SettingsPopup>();
    }
    private void OnClickQuitGameButton()
    {
        Managers.UI.ShowPopupUI<UI_QuitGamePopup>();
    }
}
