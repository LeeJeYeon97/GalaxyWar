using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Pattern_BlackHole", menuName = "BossPatterns/Pattern_BlackHole")]
public class Pattern_BlackHoleSO : BossPatternSO
{
    [Header("프리팹 연결")]
    public GameObject blackHolePrefab;

    [Header("블랙홀 패턴 설정")]
    public float fireDelay = 1.0f;
    public float bulletSpeed = 5f;     // 발사되어 날아가는 속도
    public float lifeTime = 7f;         // 블랙홀이 맵에 존재하는 총 시간
    public float pullForce = 20f;       // 빨아들이는 힘
    public float centerDamage = 20f;     // 중심부 데미지
    public float damageInterval = 0.5f; // 데미지 간격
    public float travelDistance = 5f;   //  [추가] 멈추기 전까지 날아갈 거리

    public override IEnumerator Execute(BossController boss)
    {
        if (boss._isDead || Managers.Game._player == null) yield break;

        yield return new WaitForGameTime(fireDelay);

        if (boss._isDead || Managers.Game._player == null) yield break;

        Vector2 targetPos = Managers.Game._player.transform.position;
        Vector2 shootDir = (targetPos - (Vector2)boss.transform.position).normalized;

        
        GameObject blackHoleGo = Managers.Resource.Instantiate(blackHolePrefab, boss.transform.position, Quaternion.identity);

        if (blackHoleGo != null)
        {
            if (blackHoleGo.TryGetComponent(out BossBlackHoleBulletController bhBullet))
            {
                bhBullet.lifeTime = this.lifeTime;
                bhBullet.pullForce = this.pullForce;
                bhBullet.travelDistance = this.travelDistance;
                bhBullet.centerDamage = this.centerDamage;
                bhBullet.damageInterval = this.damageInterval;

                bhBullet.Init(boss.transform.position, shootDir, bulletSpeed);
            }
        }

        yield return new WaitForGameTime(nextPatternDelay);
    }
}