using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Pattern_Sniper", menuName = "BossPatterns/Pattern_Sniper")]
public class Pattern_SniperSO : BossPatternSO
{
    [Header("저격 패턴 설정")]
    public int burstCount = 5;        // 한 번 조준할 때 쏘는 총알 수 (예: 5점사)
    public float fireDelay = 0.08f;   // 연사 속도 (매우 빠르게!)
    public int repeatCount = 3;       // 이 짓을 몇 번 반복할 것인가
    public float repeatDelay = 1.2f;  // 다음 조준까지의 대기 시간
    public float bulletSpeed = 12f;   // 일반 총알보다 훨씬 빨라야 합니다!

    public override IEnumerator Execute(BossController boss)
    {
        for (int i = 0; i < repeatCount; i++)
        {
            if (boss._isDead || boss.attackTarget == null) yield break;

            // 1. 발사 직전, 플레이어의 현재 위치를 가져와서 방향을 계산 (정조준!)
            Vector2 targetPos = boss.attackTarget.transform.position;
            Vector2 shootDir = (targetPos - (Vector2)boss.firePoint.position).normalized;

            // 2. 조준한 방향으로 다다다당! 연사
            for (int j = 0; j < burstCount; j++)
            {
                if (boss._isDead) yield break;

                boss.FireBullet(shootDir, bulletSpeed);
                yield return new WaitForGameTime(fireDelay);
            }

            // 3. 유저가 피할 시간을 주고 다시 조준
            yield return new WaitForGameTime(repeatDelay);
        }
    }
}