using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ReloadBar : UI_Base
{
    enum ProgressBar
    {
        ReloadBar,
    }
    enum Texts
    {
        Value,
        ReloadText,
    }

    private Image _bar;
    private TextMeshProUGUI _value;

    private TextMeshProUGUI _reloadText;

    public override void Init()
    {
        base.Init();

        Bind<Image>(typeof(ProgressBar));
        Bind<TextMeshProUGUI>(typeof(Texts));

        _bar = Get<Image>((int)ProgressBar.ReloadBar);
        _bar.fillAmount = 0.0f;
        _value = Get<TextMeshProUGUI>((int)Texts.Value);
        _value.text = "0%";

        _reloadText = Get<TextMeshProUGUI>((int)Texts.ReloadText);

        // 이벤트 연결
        Managers.Event.Subscribe<float>(Define.ActionEvent.ReloadStart,ReloadStart);
        Managers.Event.Subscribe(Define.ActionEvent.ReloadEnd,ReloadEnd);
        // 꺼놓기
        gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        Managers.Event.UnSubscribe<float>(Define.ActionEvent.ReloadStart, ReloadStart);
        Managers.Event.UnSubscribe(Define.ActionEvent.ReloadEnd, ReloadEnd);
    }
    private void ReloadStart(float reloadTime)
    {
        gameObject.SetActive(true);

        // 초기화
        _bar.fillAmount = 0.0f;
        _value.text = "0%";

        // ★ 투명도 100%로 초기화 (이전에 깜빡이다가 꺼졌을 수 있으므로)
        Color color = _bar.color;
        color.a = 1f;
        _bar.color = color;

        Color textColor = _value.color;
        textColor.a = 1f;
        _value.color = textColor;

        Color textColor1 = _reloadText.color;
        textColor1.a = 1f;
        _reloadText.color = textColor1;

        // DOTween의 DOFillAmount를 사용해 reloadTime 동안 알아서 채우게 합니다!
        _bar.DOFillAmount(1.0f, reloadTime).SetEase(Ease.Linear);

        // 텍스트는 0~100%로 자연스럽게 올라가게 연출
        DOVirtual.Float(0f, 100f, reloadTime, (v) =>
        {
            _value.text = $"{Mathf.RoundToInt(v)}%";
        }).SetEase(Ease.Linear);

        // ★ 4. 깜빡이는(Blink) 애니메이션 추가!
        // 투명도를 0.3f까지 0.2초 동안 내렸다가 다시 올리는 동작을 무한 반복(-1)합니다.
        _bar.DOFade(0.3f, 0.2f).SetLoops(-1, LoopType.Yoyo);
        _value.DOFade(0.3f, 0.2f).SetLoops(-1, LoopType.Yoyo);
        _reloadText.DOFade(0.3f, 0.2f).SetLoops(-1, LoopType.Yoyo);
    }

    private void ReloadEnd()
    {
        // 실행 중인 트윈(애니메이션) 정리 후 끄기
        _bar.DOKill();
        _value.DOKill();
        _reloadText.DOKill();

        // ★ 투명도 원상 복구 (다음 장전 때 안 보일 수 있으니 1.0으로 되돌려놓음)
        Color c = _bar.color;
        c.a = 1f;
        _bar.color = c;

        Color textColor = _value.color;
        textColor.a = 1f;
        _value.color = textColor;

        Color textColor1 = _reloadText.color;
        textColor1.a = 1f;
        _reloadText.color = textColor1;

        gameObject.SetActive(false);
    }
}
