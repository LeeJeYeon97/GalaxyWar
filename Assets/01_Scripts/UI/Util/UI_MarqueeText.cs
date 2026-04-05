using UnityEngine;
using TMPro;
using DG.Tweening; // ★ 우리의 친구 두트윈!

[RequireComponent(typeof(TextMeshProUGUI))]
public class UI_MarqueeText : UI_Base
{
    private TextMeshProUGUI _tmp;
    private RectTransform _rectTransform;

    // Tween 대신 Sequence로 변경하여 복잡한 타이밍 대본을 짭니다!
    private Sequence _scrollSequence;

    [Header("세팅")]
    public float scrollSpeed = 50f;
    public float startDelay = 1.5f; // 처음 시작할 때 대기 시간
    public float endDelay = 1.0f;   // 다 지나가고 나서 뿅 돌아가기 전 대기 시간 (새로 추가!)

    // ★ 아까 1단계에서 만든 부모 마스크 영역(DescMask)을 인스펙터에서 넣어주세요!
    public RectTransform maskRect;

    public override void Init()
    {
        if (_init)
        {
            return;
        }
        base.Init();

        _tmp = GetComponent<TextMeshProUGUI>();
        _rectTransform = GetComponent<RectTransform>();
    }
    public void PlayMarquee(string newText)
    {
        Init();

        // 1. 기존 시퀀스 확실하게 죽이기 & 텍스트/위치 초기화
        _scrollSequence?.Kill();
        _tmp.text = newText;
        _rectTransform.anchoredPosition = Vector2.zero;

        // 2. 텍스트 길이가 갱신될 때까지 1프레임 강제 업데이트
        _tmp.ForceMeshUpdate();

        // 3. 실제 글자 길이 vs 마스크(보이는 화면) 길이 비교
        float textWidth = _tmp.preferredWidth;
        float maskWidth = maskRect.rect.width;

        if (textWidth > maskWidth)
        {
            float moveDistance = textWidth - maskWidth + 20f;
            float duration = moveDistance / scrollSpeed;

            // ★ 4. 마법의 시퀀스 대본 작성!
            _scrollSequence = DOTween.Sequence();
            _scrollSequence.SetUpdate(true);
            // [대본 1장] 처음에 원래 위치에서 글자 읽을 시간 주기
            _scrollSequence.AppendInterval(startDelay);

            // [대본 2장] 왼쪽으로 설정한 속도만큼 스르륵 밀기
            _scrollSequence.Append(_rectTransform.DOAnchorPosX(-moveDistance, duration).SetEase(Ease.Linear));

            // [대본 3장] 끝까지 다 밀렸을 때 잠깐 멈춰서 마지막 단어 읽을 시간 주기
            _scrollSequence.AppendInterval(endDelay);

            // [대본 4장] 원래 위치(0)로 0초 만에 뿅! 하고 순간이동 시키기
            _scrollSequence.Append(_rectTransform.DOAnchorPosX(0, 0));

            // 이 전체 4장짜리 대본을 무한 반복!
            _scrollSequence.SetLoops(-1);
        }
    }

    private void OnDisable()
    {
        // 팝업이 꺼지면 애니메이션 확실히 죽이기 (에러 방지!)
        _scrollSequence?.Kill();
    }
}