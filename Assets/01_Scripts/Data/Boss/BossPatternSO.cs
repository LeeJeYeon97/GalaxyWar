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

    // 워프패턴
    public float fadeOutTime; // 사라지는 데 걸리는 시간
    public float fadeInTime;  // 나타나는 데 걸리는 시간
    public float warpRadius;  // NearPlayer일 경우, 플레이어 주변 몇 거리 안으로 떨어질지

    // 돌진
    public float warningTime; // 돌진 전 타겟을 응시하며 멈춰있는 시간 (경고)
    public float dashSpeed;    // 돌진하는 속도
    public float overshoot;     // 플레이어 위치를 뚫고 얼마나 더 지나갈 것인가 (여유 거리)
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