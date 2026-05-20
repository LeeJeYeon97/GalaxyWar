using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Pattern_Shotgun", menuName = "BossPatterns/Pattern_Shotgun")]
public class Pattern_ShotgunSO : BossPatternSO
{
    [Header("샷건 패턴 설정")]
    public int bulletCount = 5;       // 한 번에 쏘는 총알 개수
    public float spreadAngle = 60f;   // 부채꼴이 퍼지는 총 각도
    public int burstCount = 3;        // 연속으로 쏘는 횟수
    public float burstDelay = 0.5f;   // 쏘는 간격
    public float bulletSpeed = 7f;

    public override IEnumerator Execute(BossController boss)
    {
        // 총알 사이의 각도 간격
        float angleStep = bulletCount > 1 ? spreadAngle / (bulletCount - 1) : 0f;

        for (int i = 0; i < burstCount; i++)
        {
            // 타겟(플레이어)이 파괴되었거나 보스가 죽었으면 즉시 중지
            if (boss._isDead || boss.attackTarget == null) yield break;

            //  1. 플레이어를 향하는 기준 각도(Base Angle) 구하기
            Vector2 targetPos = boss.attackTarget.transform.position;
            Vector2 dirToTarget = (targetPos - (Vector2)boss.firePoint.position).normalized;

            // Atan2를 사용해 방향 벡터를 360도 각도로 변환합니다.
            float baseAngle = Mathf.Atan2(dirToTarget.y, dirToTarget.x) * Mathf.Rad2Deg;

            //  2. 플레이어 방향(baseAngle)을 정중앙으로 두고, 절반만큼 빼서 시작 각도를 잡습니다.
            float startAngle = baseAngle - (spreadAngle / 2f);

            for (int j = 0; j < bulletCount; j++)
            {
                // 부채꼴 내에서의 현재 총알 각도
                float currentAngle = startAngle + (angleStep * j);

                // 다시 각도를 방향 벡터(슈팅 방향)로 변환
                float rad = currentAngle * Mathf.Deg2Rad;
                Vector2 shootDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                boss.FireBullet(shootDir, bulletSpeed);
            }

            // 한 번 쏘고 설정된 시간만큼 대기 (다음 번 쏠 땐 플레이어 위치를 다시 조준!)
            yield return new WaitForSeconds(burstDelay);
        }
    }
}