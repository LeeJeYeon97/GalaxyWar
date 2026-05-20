using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_BossHpBarPopup : UI_Popup
{
    enum Images
    {
        Image_Fill // 깎이는 붉은색 체력바 이미지
    }

    enum Texts
    {
        Text_Hp // (선택) 보스 이름
    }

    private BossController _targetBoss;
    private Image _fillImage;

    public override void Init()
    {
        base.Init();

        Bind<Image>(typeof(Images));
        Bind<TMP_Text>(typeof(Texts));

        _fillImage = GetImage((int)Images.Image_Fill);
        _fillImage.fillAmount = 1f; // 처음엔 꽉 차게!
    }

    // 보스가 스폰될 때 자기 자신을 연결해주는 함수
    public void SetTargetBoss(BossController boss, string bossName = "UNKNOWN")
    {
        if(_init == false)
        {
            Init();
        }

        _targetBoss = boss;

        //  핵심: 보스의 알람(이벤트)에 내 귀를 귀울입니다(구독).
        // 주의: 중복 구독을 막기 위해 항상 빼기(-=)를 먼저 하고 더하기(+=)를 하는 것이 정석입니다!

        GetTMP((int)Texts.Text_Hp).text = $"{boss.currentHp} / {boss.Stat.MaxHp.TotalValue}";

        _targetBoss.OnHpChanged -= UpdateHpBar;
        _targetBoss.OnHpChanged += UpdateHpBar;
    }

    // 보스가 OnHpChanged?.Invoke(...)를 부를 때마다 이 함수가 자동으로 실행됩니다!
    private void UpdateHpBar(float currentHp, float maxHp)
    {
        float targetFill = currentHp / maxHp;


        GetTMP((int)Texts.Text_Hp).text = $"{currentHp} / {maxHp}";

        //  Update문의 Lerp 대신 DOTween의 DOFillAmount를 씁니다!
        // 0.2초 동안 타겟 수치까지 부드럽게 깎입니다.
        _fillImage.DOFillAmount(targetFill, 0.2f).SetEase(Ease.OutQuad).SetLink(gameObject);

        // 보스가 죽었을 때 팝업 닫기 처리도 여기서 함께 해줍니다.
        if (currentHp <= 0)
        {
            Managers.UI.ClosePopupUI(this);
        }
    }

    //  [매우 중요] 이벤트(Action)를 썼다면 오브젝트가 파괴될 때 반드시 귀를 막아줘야(구독 해제) 메모리 누수가 안 생깁니다!
    private void OnDestroy()
    {
        if (_targetBoss != null)
        {
            _targetBoss.OnHpChanged -= UpdateHpBar;
        }
    }
}