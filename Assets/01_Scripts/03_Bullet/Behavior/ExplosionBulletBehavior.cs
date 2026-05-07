using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class ExplosionBulletBehavior : IBulletBehavior
{

    public void OnHit(BulletController bullet, GameObject target, BaseBulletStat activeStat)
    {
        if (target == null) return;


        if (activeStat is ExplosionBulletStat stat)
        {
            float radius = stat.explosionRange.TotalValue;
            float finalExplosionDmg = stat.damage.TotalValue * stat.explosionDamage.TotalValue;

            int layerMask = 1 << LayerMask.NameToLayer("Meteor");

            // 핵심 1: 중복 데미지 및 무한 연쇄 폭발을 막기 위한 '블랙리스트'
            HashSet<GameObject> damagedMeteors = new HashSet<GameObject>();

            // 최초 타겟(직접 맞은 놈)은 총알 기본 데미지를 받았을 테니 명단에 미리 넣습니다.
            damagedMeteors.Add(target);

            // ==========================================
            // 1. 1차 폭발 로직
            // ==========================================
            Collider2D[] primaryColliders = Physics2D.OverlapCircleAll(target.transform.position, radius, layerMask);

            
            // 2차 폭발의 '진원지'가 될 메테오들을 모아둘 리스트
            List<MeteorController> secondaryTargets = new List<MeteorController>();
            Managers.Sound.Play(Define.SoundID.Sfx_Explosion_Hit);
            foreach (var col in primaryColliders)
            {
                // 이미 맞은 놈(최초 타겟 포함)은 제외
                if (damagedMeteors.Contains(col.gameObject)) continue;

                MeteorController meteor = col.GetComponent<MeteorController>();
                if (meteor)
                {
                    // (참고: 이전 단계에서 모듈화를 하셨다면 meteor.Health.OnDamage() 로 변경하세요!)
                    bullet.CalculateDamage(meteor, finalExplosionDmg);

                    damagedMeteors.Add(col.gameObject); // 맞았다고 명단에 기록
                    secondaryTargets.Add(meteor);       // 2차 폭발 진원지로 등록
                }
            }

            // ==========================================
            // 2. 2차 연쇄 폭발 로직 (5레벨 이상일 때 특수 능력!)
            // ==========================================
            // 주의: Stat 스크립트에 Level 변수가 있다고 가정했습니다. 실제 쓰시는 변수명으로 바꿔주세요)
            if (stat.curLevel >= 5)
            {
                foreach (var secTarget in secondaryTargets)
                {
                    // secTarget이 1차 폭발 데미지로 죽었더라도, 그 '위치'에서는 폭발이 일어나야 합니다.
                    Vector2 secExplosionPos = secTarget.transform.position;

                    // (선택 사항) 여기서 2차 폭발 전용 작은 파티클 이펙트를 터트려주면 타격감이 엄청납니다!
                    bullet.BulletParticle.SpawnHit(secExplosionPos, Vector2.zero, stat);
                    Managers.Sound.Play(Define.SoundID.Sfx_Explosion_Hit);
                    //Managers.Resource.Instantiate("SecondaryExplosionEffect", secExplosionPos);

                    Collider2D[] secondaryColliders = Physics2D.OverlapCircleAll(secExplosionPos, radius, layerMask);

                    foreach (var col in secondaryColliders)
                    {
                        // 1차 폭발 때 맞았거나, 다른 2차 폭발로 이미 맞은 놈은 또 맞지 않음!
                        if (damagedMeteors.Contains(col.gameObject)) continue;

                        MeteorController meteor = col.GetComponent<MeteorController>();
                        if (meteor)
                        {
                            // 2차 폭발 데미지 (밸런스를 위해 finalExplosionDmg * 0.5f 처럼 반감시켜도 좋습니다)
                            meteor.OnDamage(finalExplosionDmg * 0.5f);

                            damagedMeteors.Add(col.gameObject); // 명단에 추가
                        }
                    }
                }
            }
        }
    }
    public void OnInit(BulletController bullet, BaseBulletStat activeStat)
    {
        
    }
    public void OnRelease(BulletController bullet)
    {
        
    }
    public void OnShot(BulletController bullet)
    {
        
    }
    public void OnUpdate(BulletController bullet)
    {
        
    }
}
