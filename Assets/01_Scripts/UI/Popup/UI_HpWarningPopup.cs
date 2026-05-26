using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UI_HpWarningPopup : UI_Popup
{
    // 경고용 이미지를 인스펙터가 아닌 코드로 바인딩하기 위한 Enum
    enum Images
    {
        Image_WarningBackground,
    }

    [Header("깜빡임 세팅")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.6f;   // 최고로 진해질 때의 투명도 (0.0 ~ 1.0)
    public float flashSpeed = 0.5f; // 한 번 진해지는데 걸리는 시간 (빠를수록 급박함)

    private Image _warningImage;
    private Tween _flashTween;

    public override void Init()
    {
        base.Init();

        // 1. 이미지 바인딩
        Bind<Image>(typeof(Images));
        _warningImage = GetImage((int)Images.Image_WarningBackground);

        // 2. 초기 상태는 완전 투명(Alpha 0)으로 세팅해서 안 보이게 만듭니다.
        Color startColor = _warningImage.color;
        startColor.a = 0f;
        _warningImage.color = startColor;
    }

    /// <summary>
    /// HP 위험 상태일 때 깜빡임 시작! (값을 안 넣으면 인스펙터 기본값 사용)
    /// </summary>
    public void PlayWarning(float targetAlpha = -1f, float speed = -1f)
    {
        if (!_init) Init();

        // 파라미터가 들어왔다면 덮어쓰고, 아니면 기본 설정값을 씁니다.
        float finalAlpha = targetAlpha >= 0f ? targetAlpha : maxAlpha;
        float finalSpeed = speed >= 0f ? speed : flashSpeed;

        // 혹시 이미 깜빡이고 있다면 기존 트윈을 죽입니다 (중복 방지)
        _flashTween?.Kill();

        //  DOTween 마법: Alpha 값을 finalAlpha까지 finalSpeed초 동안 올립니다.
        // SetLoops(-1, LoopType.Yoyo) -> 영원히(-1) 왔다 갔다(Yoyo) 반복합니다!
        _flashTween = _warningImage.DOFade(finalAlpha, finalSpeed)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine) // 심장 박동처럼 부드럽게 곡선 처리
            .SetUpdate(true);        // 게임 일시정지 중에도 깜빡이게 할지 여부
    }

    /// <summary>
    /// 포션을 먹었거나 위험을 벗어났을 때 깜빡임 정지!
    /// </summary>
    public void StopWarning()
    {
        if (!_init) Init();

        // 깜빡임 루프를 강제로 멈춥니다.
        _flashTween?.Kill();

        // 멈추자마자 뚝 끊기면 어색하므로 0.3초 동안 스르륵 투명해지며 사라지게 합니다.
        _warningImage.DOFade(0f, 0.3f).SetUpdate(true);

        Managers.UI.ClosePopupUI(this);
    }

    private void OnDestroy()
    {
        // 팝업이 파괴될 때 찌꺼기 트윈이 남아서 에러를 뿜는 것을 완벽 방어!
        _flashTween?.Kill();
    }
}