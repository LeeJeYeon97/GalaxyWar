using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    private TMP_Text _textMesh;

    private void Awake()
    {
        _textMesh = GetComponent<TMP_Text>();
    }

    // 메테오가 맞았을 때 외부에서 호출해 줄 초기화 함수
    public void Init(Vector3 spawnPos, int damage, bool isCritical = false)
    {
        // ★ 아주 중요: 풀링으로 재사용하기 때문에, 이전에 실행 중이던 애니메이션을 확실히 꺼줘야 합니다!
        transform.DOKill();
        _textMesh.DOKill();

        // 1. 위치와 텍스트 세팅
        transform.position = spawnPos;
        _textMesh.text = damage.ToString();
        // 투명도 복구 (알파값 1)
        Color color = _textMesh.color;
        color.a = 1f;

        // 2. 크리티컬 여부에 따른 시각적 분기 처리
        if (isCritical)
        {
            // 크리티컬: 빨간색, 기본 크기의 1.5배
            color.r = 1f; color.g = 0.2f; color.b = 0.2f; // 강렬한 붉은색
            transform.localScale = Vector3.one * 1.5f;

            // 타격감 넘치는 펀치 효과! (커졌다가 통통 튀며 돌아옴)
            // 인자: (펀치 강도, 지속 시간, 진동 횟수, 탄성)
            transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 5, 1f);
        }
        else
        {
            // 일반 타격: 하얀색, 기본 크기(1배)
            color.r = 1f; color.g = 1f; color.b = 1f;
            transform.localScale = Vector3.one;
        }

        _textMesh.color = color;

        // 3. 공통 이동 및 페이드아웃 애니메이션
        // 위로 살짝 올라가기
        transform.DOMoveY(spawnPos.y + 0.3f, 1.0f).SetEase(Ease.OutQuad);

        // 0.2초 대기 후 0.3초 동안 서서히 투명해지기
        _textMesh.DOFade(0f, 0.5f).SetDelay(0.5f).OnComplete(() =>
        {
            // 애니메이션이 끝나면 반납
            Managers.Resource.Destroy(this.gameObject);
        });
    }
}
