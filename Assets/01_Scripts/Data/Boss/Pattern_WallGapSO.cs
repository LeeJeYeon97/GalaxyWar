using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Pattern_WallGap", menuName = "BossPatterns/Pattern_WallGap")]
public class Pattern_WallGapSO : BossPatternSO
{
    [Header("장벽 패턴 설정")]
    public int totalBullets = 20;     // 부채꼴을 구성하는 전체 총알 수 (촘촘할수록 압박감 상승)
    public float spreadAngle = 160f;  // 화면을 덮을 넓은 각도
    public int gapSize = 3;           // 구멍의 크기 (총알 몇 개를 뺄 것인가?)
    public int waveCount = 3;         // 장벽을 몇 번 연속으로 보낼 것인가?
    public float waveDelay = 1.5f;    // 장벽 사이의 간격
    public float bulletSpeed = 4f;    // 유저가 구멍을 찾을 수 있게 속도는 약간 느리게!

    public override IEnumerator Execute(BossController boss)
    {
        float startAngle = -90f - (spreadAngle / 2f);
        float angleStep = spreadAngle / (totalBullets - 1);

        for (int w = 0; w < waveCount; w++)
        {
            if (boss._isDead) yield break;

            // 랜덤하게 구멍이 시작될 인덱스를 뽑습니다. 
            // (양쪽 끝단에 구멍이 생기면 너무 쉬우므로 중간 어딘가로 제한)
            int gapStartIndex = Random.Range(2, totalBullets - gapSize - 2);

            for (int i = 0; i < totalBullets; i++)
            {
                // i가 구멍 인덱스 범위 안에 포함되면 총알을 쏘지 않고(구멍 생성) 스킵!
                if (i >= gapStartIndex && i < gapStartIndex + gapSize)
                {
                    continue;
                }

                float currentAngle = startAngle + (angleStep * i);
                float rad = currentAngle * Mathf.Deg2Rad;
                Vector2 shootDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                boss.FireBullet(shootDir, bulletSpeed);
            }

            yield return new WaitForSeconds(waveDelay);
        }
    }
}