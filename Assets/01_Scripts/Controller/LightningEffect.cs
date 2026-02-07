using System.Collections;
using UnityEngine;

public class LightningEffect : MonoBehaviour
{
    private LineRenderer _line;
    [SerializeField] private float _duration = 0.2f; // 번개가 번쩍이는 시간


    void Awake()
    {
        _line = GetComponent<LineRenderer>();
    }

    // 외부에서 호출: 시작점과 끝점을 받음
    public void PlayEffect(Vector3 startPos, Vector3 endPos)
    {
        _line.positionCount = 2;
        _line.SetPosition(0, startPos);
        _line.SetPosition(1, endPos);

        // 약간 지글거리는 효과를 위해 중간 점을 추가해도 좋음 (심화)
        StartCoroutine(DisableRoutine());
    }

    IEnumerator DisableRoutine()
    {
        // 0.2초 뒤에 풀로 반납
        yield return new WaitForSeconds(_duration);
        Managers.Pool.Release(this.gameObject);
    }
}
