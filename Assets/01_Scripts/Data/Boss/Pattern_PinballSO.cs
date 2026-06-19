using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Pattern_Pinball", menuName = "BossPatterns/Pattern_Pinball")]
public class Pattern_PinballSO : BossPatternSO
{
    [Header("핀볼 패턴 설정")]
    public int totalBullets = 16;     // 쏠 총알의 총 개수
    public float fireDelay = 0.15f;   // 한 발 쏠 때마다의 딜레이
    public float bulletSpeed = 8f;

    public override IEnumerator Execute(BossController boss)
    {
        // 10시 방향(왼쪽 위)과 2시 방향(오른쪽 위)의 벡터
        Vector2 leftUpDir = new Vector2(-1f, 1f).normalized;
        Vector2 rightUpDir = new Vector2(1f, 1f).normalized;

        for (int i = 0; i < totalBullets; i++)
        {
            if (boss._isDead) yield break;

            // 짝수 번째는 왼쪽 위로, 홀수 번째는 오른쪽 위로 번갈아가며 발사!
            Vector2 shootDir = (i % 2 == 0) ? leftUpDir : rightUpDir;

            boss.FireBullet(shootDir, bulletSpeed);

            yield return new WaitForGameTime(fireDelay);
        }
    }
}