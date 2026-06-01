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
        // 총알 사이의 각도 간격은 변하지 않으므로 밖에서 미리 계산해둡니다.
        float angleStep = spreadAngle / (totalBullets - 1);

        for (int w = 0; w < waveCount; w++)
        {
            // 타겟(플레이어)이 파괴되었거나 보스가 죽었으면 즉시 중지
            if (boss._isDead || boss.attackTarget == null) yield break;

            //  1. 플레이어를 향하는 기준 각도(Base Angle) 구하기
            Vector2 targetPos = boss.attackTarget.transform.position;
            Vector2 dirToTarget = (targetPos - (Vector2)boss.firePoint.position).normalized;
            float baseAngle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;

            //  2. 플레이어 방향(baseAngle)을 정중앙으로 두고, 절반만큼 빼서 부채꼴 시작 각도를 잡습니다.
            float startAngle = baseAngle - (spreadAngle / 2f);

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

                // 부채꼴 내에서의 현재 총알 각도 계산
                float currentAngle = startAngle + (angleStep * i);
                float rad = currentAngle * Mathf.Deg2Rad;
                Vector2 shootDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                boss.FireBullet(shootDir, bulletSpeed);
            }

            // 한 번 쏘고 설정된 시간만큼 대기 (다음 번 쏠 땐 플레이어 위치를 '다시' 조준합니다!)
            yield return new WaitForSeconds(waveDelay);
        }
    }
}