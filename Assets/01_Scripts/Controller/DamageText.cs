using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    private TextMeshPro _textMesh;

    private void Awake()
    {
        _textMesh = GetComponent<TextMeshPro>();
    }

    // 메테오가 맞았을 때 외부에서 호출해 줄 초기화 함수
    public void Init(Vector3 spawnPos, int damage)
    {
        // 1. 위치와 텍스트 세팅
        transform.position = spawnPos;
        _textMesh.text = damage.ToString();

        // 투명도를 다시 100%(불투명)로 초기화
        Color color = _textMesh.color;
        color.a = 1f;
        _textMesh.color = color;

        // 2. DOTween 애니메이션 연출
        // 위로 1만큼 0.5초 동안 이동
        transform.DOMoveY(spawnPos.y + 0.5f, 0.5f).SetEase(Ease.OutQuad);

        // 0.5초 동안 서서히 투명해지기
        _textMesh.DOFade(0f, 1.0f).OnComplete(() =>
        {
            // 애니메이션이 끝나면 스스로 풀링 매니저로 반환!
            Managers.Pool.Release(this.gameObject);
        });
    }
}
