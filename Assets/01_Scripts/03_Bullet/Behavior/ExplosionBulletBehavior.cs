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

            //  1. 레이어 마스크 업데이트: 적(보스, 엘리트)과 메테오 레이어를 모두 포함시킵니다!
            // (유니티에 세팅하신 실제 레이어 이름들을 콤마로 연결해서 넣어주세요)
            int layerMask = LayerMask.GetMask("Meteor", "Boss");

            // 핵심 1: 중복 데미지 및 무한 연쇄 폭발을 막기 위한 '블랙리스트'
            HashSet<GameObject> damagedTargets = new HashSet<GameObject>();

            // 최초 타겟(직접 맞은 놈) 명단에 미리 넣기
            damagedTargets.Add(target);

            // ==========================================
            // 1. 1차 폭발 로직
            // ==========================================
            Collider2D[] primaryColliders = Physics2D.OverlapCircleAll(target.transform.position, radius, layerMask);

            // 2. 2차 폭발의 '진원지' 역할을 할 위치(Transform)를 모아둘 리스트
            // (IDamageable은 인터페이스라 transform 속성이 없으므로, Transform을 직접 저장하는 것이 깔끔합니다)
            List<Transform> secondaryExplosionCenters = new List<Transform>();
            Managers.Sound.Play(Define.SoundID.Sfx_Explosion_Hit);

            foreach (var col in primaryColliders)
            {
                // 이미 맞은 놈(최초 타겟 포함)은 제외
                if (damagedTargets.Contains(col.gameObject)) continue;

                // 3. 대상이 메테오인지 보스인지 묻지도 따지지도 않고 인터페이스만 추출!
                IDamageable damageable = col.GetComponent<IDamageable>();

                if (damageable != null)
                {
                    // 인터페이스를 통해 공평하게 데미지를 입힙니다.
                    // (만약 bullet.CalculateDamage를 계속 쓰고 싶으시다면 해당 함수의 매개변수도 IDamageable로 수정하시면 됩니다!)
                    bullet.CalculateDamage(damageable, finalExplosionDmg);
                    
                    damagedTargets.Add(col.gameObject);         // 맞았다고 명단에 기록
                    secondaryExplosionCenters.Add(col.transform); // 2차 폭발 진원지로 위치 등록
                }
            }

            // ==========================================
            // 2. 2차 연쇄 폭발 로직 (5레벨 이상일 때 특수 능력!)
            // ==========================================
            if (stat.curLevel >= 5)
            {
                foreach (var secCenter in secondaryExplosionCenters)
                {
                    //  4. 진원지의 위치를 가져옵니다. 
                    // (대상이 이미 죽어서 오브젝트가 파괴되었을 위험이 있으므로, 안전장치를 하나 걸어줍니다)
                    if (secCenter == null) continue;
                    Vector2 secExplosionPos = secCenter.position;

                    // 2차 폭발 파티클 및 사운드
                    bullet.BulletParticle?.SpawnHit(secExplosionPos, Vector2.zero, stat);
                    Managers.Sound.Play(Define.SoundID.Sfx_Explosion_Hit);

                    Collider2D[] secondaryColliders = Physics2D.OverlapCircleAll(secExplosionPos, radius, layerMask);

                    foreach (var col in secondaryColliders)
                    {
                        // 1차 폭발 때 맞았거나, 다른 2차 폭발로 이미 맞은 놈은 또 맞지 않음!
                        if (damagedTargets.Contains(col.gameObject)) continue;

                        IDamageable damageable = col.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            // 2차 폭발 데미지 적용
                            bullet.CalculateDamage(damageable, finalExplosionDmg * 0.5f);
                            
                            damagedTargets.Add(col.gameObject); // 명단에 추가
                        }
                    }
                }
            }
        }
    }

    public void OnInit(BulletController bullet, BaseBulletStat activeStat) { }
    public void OnRelease(BulletController bullet) { }
    public void OnShot(BulletController bullet) { }
    public void OnUpdate(BulletController bullet) { }
}
