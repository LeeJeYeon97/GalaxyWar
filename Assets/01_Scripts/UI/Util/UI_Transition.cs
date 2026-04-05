using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_Transition : MonoBehaviour
{
    // 씬을 이동할 때 이 함수를 부릅니다!
    public Image fadeImage;

    private void Awake()
    {
        if(fadeImage.gameObject.activeSelf == true)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }
    // 화면을 까맣게 덮는 함수 (완료되면 onComplete 실행)
    public void FadeOut(Action onComplete)
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 0);

        // 0.5초 동안 까매지고, 끝나면 감독(SceneManager)에게 알려줌
        fadeImage.DOFade(1f, 1.0f).SetUpdate(true).OnComplete(() =>
        {
            if (onComplete != null) onComplete.Invoke();
        });
    }

    public void FadeIn()
    {
        // 그냥 냅다 실행하지 말고, 코루틴으로 숨 고르기 시작!
        StartCoroutine(WaitAndFadeInRoutine());
    }

    private IEnumerator WaitAndFadeInRoutine()
    {
        // 핵심 마법: 씬 로딩 직후 발생하는 최악의 렉 구간(첫 프레임들)을 흘려보냅니다.
        // yield return null 한 번당 1프레임을 쉽니다. 3프레임 정도면 렉이 충분히 풀립니다!
        yield return null;
        yield return null;
        yield return null;

        // 이제 유니티 엔진이 안정화되었으니, 1초 동안 부드럽게 커튼을 걷어냅니다!
        fadeImage.DOFade(0f, 1.0f).SetUpdate(true).OnComplete(() =>
        {
            fadeImage.gameObject.SetActive(false);
        });
    }
}
