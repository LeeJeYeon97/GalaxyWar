using UnityEngine;
using DG.Tweening; // DOTween 활용

public class ScreenEffectManager : MonoBehaviour
{
    // 싱글톤으로 만들면 어디서든 부르기 편합니다.
    public static ScreenEffectManager Instance;

    [SerializeField] private Material glitchMaterial;
    private readonly int _intensityID = Shader.PropertyToID("_Intensity");

    void Awake()
    {
        Instance = this;
        // 시작할 때는 효과 끄기
        glitchMaterial.SetFloat(_intensityID, 0f);
    }

    // 플레이어가 피격되었을 때 호출할 함수
    public void PlayHitGlitch(float duration = 0.1f, float strength = 1f)
    {
        // 이전 트윈이 돌고 있다면 중지
        DOTween.Kill(glitchMaterial);

        // 0 -> strength -> 0 으로 아주 빠르게 왕복 (Yoyo)
        // 0.05초 만에 켜지고 0.05초 만에 꺼지게 설정
        DOTween.To(() => glitchMaterial.GetFloat(_intensityID),
                   x => glitchMaterial.SetFloat(_intensityID, x),
                   strength, duration * 0.5f)
               .SetLoops(2, LoopType.Yoyo)
               .SetEase(Ease.OutFlash)
               .SetUpdate(true); // 일시정지 중에도 연출은 나와야 함
    }

    private void OnDestroy()
    {
        // 게임 종료 시 마테리얼 수치 초기화 (에디터에 남는 것 방지)
        glitchMaterial.SetFloat(_intensityID, 0f);
    }
}