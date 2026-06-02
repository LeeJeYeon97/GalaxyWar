using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PatternConfigWrapper
{
    public List<PatternBalanceData> patternList;
}

[System.Serializable]
public struct PatternBalanceData
{
    //  핵심: 열거형을 받을 문자열 변수 추가
    public string type;

    public string patternName;
    public float nextPatternDelay;

    // [패턴별 수치들]
    public int bulletCount;
    public int totalBullets;
    public int burstCount;
    public int repeatCount;
    public int gapSize;
    public int waveCount;

    public float burstDelay;
    public float fireDelay;
    public float repeatDelay;
    public float waveDelay;

    public float bulletSpeed;
    public float spreadAngle;
    public float angleStep;
}

// 모든 보스 패턴의 뼈대가 되는 추상 클래스
public abstract class BossPatternSO : ScriptableObject
{
    [Header("패턴 기본 설정")]
    public Define.BossPatternType type;
    public string patternName;
    public float nextPatternDelay = 2.5f; // 이 패턴이 끝나고 쉴 시간

    // 자식 클래스들이 무조건 구현해야 하는 핵심 실행 함수
    // 코루틴을 돌리기 위해 BossController 자기 자신(this)을 넘겨받습니다!
    public abstract IEnumerator Execute(BossController boss);
}