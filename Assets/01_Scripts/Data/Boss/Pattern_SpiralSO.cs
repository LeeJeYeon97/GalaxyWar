using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Pattern_Spiral", menuName = "BossPatterns/Pattern_Spiral")]
public class Pattern_SpiralSO : BossPatternSO
{
    [Header("나선환 상세 설정")]
    public int bulletCount = 30;
    public float angleStep = 15f;
    public float fireDelay = 0.1f;
    public float bulletSpeed = 5f;

    public override IEnumerator Execute(BossController boss)
    {
        Debug.Log($"[{boss.gameObject.name}] 패턴 시전: {patternName}");
        float currentAngle = 0f;

        for (int i = 0; i < bulletCount; i++)
        {
            if (boss._isDead) yield break; // 보스가 죽었으면 즉시 중지

            float rad = currentAngle * Mathf.Deg2Rad;
            Vector2 shootDir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            // 보스가 가진 FireBullet 기능을 빌려서 쏩니다!
            boss.FireBullet(shootDir, bulletSpeed);

            currentAngle += angleStep;
            yield return new WaitForGameTime(fireDelay);
        }
    }
}