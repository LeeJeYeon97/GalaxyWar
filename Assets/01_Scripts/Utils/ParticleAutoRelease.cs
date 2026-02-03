using UnityEngine;

public class ParticleAutoRelease : MonoBehaviour
{
    private ParticleSystem _ps;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();

        // 메인 모듈에서 Stop Action을 'None'으로 설정해야 코드가 제어하기 쉽습니다.
        var main = _ps.main;
        main.stopAction = ParticleSystemStopAction.None;
    }

    void OnEnable()
    {
        // 꺼내지는 순간 재생
        _ps.Play();
        // 재생 완료 체크 코루틴 시작
        StartCoroutine(CoCheckFinished());
    }

    System.Collections.IEnumerator CoCheckFinished()
    {
        // 파티클이 살아있는 동안 대기 (입자가 하나라도 남아있으면 True)
        while (_ps.IsAlive(true))
        {
            yield return new WaitForSeconds(0.2f); // 매 프레임 검사할 필요 없음
        }

        // 재생이 끝났으므로 풀로 반납
        Managers.Pool.Release(this.gameObject);
    }
}
