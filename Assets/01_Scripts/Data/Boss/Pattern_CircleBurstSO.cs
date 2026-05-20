using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Pattern_CircleBurst", menuName = "BossPatterns/Pattern_CircleBurst")]
public class Pattern_CircleBurstSO : BossPatternSO
{
    [Header("원형 폭발 패턴 설정")]
    public int bulletCount = 18;      // 360도를 몇 갈래로 쪼갤 것인가 (예: 18이면 20도 간격)
    public int burstCount = 2;        // 몇 번 펑! 펑! 터트릴 것인가
    public float burstDelay = 1.0f;   // 터트리는 간격
    public float bulletSpeed = 6f;

    public override IEnumerator Execute(BossController boss)
    {
        // 360도를 총알 개수로 나누어 각도 간격을 구합니다.
        float angleStep = 360f / bulletCount;

        for (int i = 0; i < burstCount; i++)
        {
            if (boss._isDead) yield break;

            for (int j = 0; j < bulletCount; j++)
            {
                float currentAngle = angleStep * j;
                float rad = currentAngle * Mathf.Deg2Rad;

                Vector2 shootDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                boss.FireBullet(shootDir, bulletSpeed);
            }

            yield return new WaitForSeconds(burstDelay);
        }
    }
}