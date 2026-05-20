using System.Collections;
using UnityEngine;

// 모든 보스 패턴의 뼈대가 되는 추상 클래스
public abstract class BossPatternSO : ScriptableObject
{
    [Header("패턴 기본 설정")]
    public string patternName;
    public float nextPatternDelay = 2.5f; // 이 패턴이 끝나고 쉴 시간

    // 자식 클래스들이 무조건 구현해야 하는 핵심 실행 함수
    // 코루틴을 돌리기 위해 BossController 자기 자신(this)을 넘겨받습니다!
    public abstract IEnumerator Execute(BossController boss);
}