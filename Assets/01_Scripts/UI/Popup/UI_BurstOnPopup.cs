using UnityEngine;
using DG.Tweening;

public class UI_BurstOnPopup : UI_Popup
{
    enum GameObjects
    {
        Image_Burst
    }

    [Header("애니메이션 설정")]
    [Tooltip("들어올 때 걸리는 시간")]
    public float slideInDuration = 0.4f;
    [Tooltip("화면에 머무는 시간")]
    public float displayDuration = 1.2f;
    [Tooltip("나갈 때 걸리는 시간")]
    public float slideOutDuration = 0.3f;

    private RectTransform _bannerRect;
    private Sequence _animSequence;

    public override void Init()
    {
        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        _bannerRect = GetObject((int)GameObjects.Image_Burst).GetComponent<RectTransform>();

        PlayBurstAnimation();
    }

    private void PlayBurstAnimation()
    {
        _animSequence?.Kill();

        // 1. 대표님이 기억하신 그 코드! 캔버스의 실제 렌더링 해상도를 가져옵니다.
        RectTransform canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float bannerWidth = _bannerRect.rect.width;

        float offScreenPosX; // 화면 밖(숨는 곳) 좌표
        float targetPosX;    // 화면 안(보여줄 곳) 좌표

        //  2. 하드코딩 제거: 앵커 상태를 파악해서 스마트하게 오프셋을 계산합니다.
        if (_bannerRect.anchorMin.x == 1f && _bannerRect.anchorMax.x == 1f)
        {
            // [상황 A] 이전에 제가 추천해 드린 대로 앵커를 '우측(Right)'에 맞춘 경우
            // 우측 끝(0) 기준으로 배너 너비만큼만 더 밀어버리면 완벽하게 화면 밖입니다!
            offScreenPosX = bannerWidth + 50f; // 50f는 안전 여백
            targetPosX = 150f;                 // 들어왔을 땐 우측 끝에서 20f 만큼 떨어지게 배치
        }
        else
        {
            // [상황 B] 앵커가 '정중앙(Center)'에 있는 경우
            // 화면 절반 크기 + 배너 절반 크기 + 여백 = 화면 우측 완전 밖!
            offScreenPosX = (canvasWidth * 0.5f) + (bannerWidth * 0.5f) + 50f;
            targetPosX = (canvasWidth * 0.5f) - (bannerWidth * 0.5f) - 20f;
        }

        // 3. 시작 위치 세팅: 계산된 화면 오른쪽 밖으로 치워둡니다.
        _bannerRect.anchoredPosition = new Vector2(offScreenPosX, _bannerRect.anchoredPosition.y);

        // 4. 시퀀스 생성 (콤보 타임라인)
        _animSequence = DOTween.Sequence();
        _animSequence.SetUpdate(true);

        // 콤보 1: 화면 안(targetPosX)으로 찰지게 들어오기
        _animSequence.Append(_bannerRect.DOAnchorPosX(targetPosX, slideInDuration).SetEase(Ease.OutBack));

        // 콤보 2: 머물기
        _animSequence.AppendInterval(displayDuration);

        // 콤보 3: 다시 밖(offScreenPosX)으로 스르륵 빠져나가기
        _animSequence.Append(_bannerRect.DOAnchorPosX(offScreenPosX, slideOutDuration).SetEase(Ease.InCubic));

        // 콤보 4: 모두 끝나면 팝업 닫기
        _animSequence.OnComplete(() =>
        {
            Managers.UI.ClosePopupUI(this);
        });
    }

    private void OnDestroy()
    {
        _animSequence?.Kill();
    }
}