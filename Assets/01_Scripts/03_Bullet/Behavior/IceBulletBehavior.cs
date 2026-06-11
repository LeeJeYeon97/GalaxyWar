using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class IceBulletBehavior : IBulletBehavior
{

    public void OnHit(BulletController bullet, GameObject target, BaseBulletStat activeStat)
    {
        if (target == null) return;

        // 직접 맞은 타겟 확인
        MeteorController meteor = target.GetComponent<MeteorController>();
        if (meteor == null) return;

        if (activeStat is IceBulletStat stat)
        {
            Managers.Sound.Play(Define.SoundID.Sfx_IceBullet_Hit);

            // =======================================================
            // 1. 1~5레벨 공통: 타겟 및 주변 적들에게 얼음 효과(AoE Ice)
            // =======================================================
            // 광역 슬로우 및 빙결이 적용될 반경
            float spreadRadius = 1.5f;
            

            // FireBullet과 동일하게 "Meteor", "Boss" 레이어를 감지하도록 설정
            int layerMask = LayerMask.GetMask("Meteor", "Boss");

            // 타겟 위치를 중심으로 spreadRadius 반경 내의 모든 콜라이더를 찾습니다.
            Collider2D[] colliders = Physics2D.OverlapCircleAll(target.transform.position, spreadRadius, layerMask);

            Managers.Effect.Play(Define.EffectType.IceBullet_Hit, target.transform.position);

            foreach (Collider2D col in colliders)
            {
                // 찾은 콜라이더가 몬스터(메테오)라면 얼음 효과를 입힙니다.
                MeteorController nearbyMeteor = col.GetComponent<MeteorController>();
                if (nearbyMeteor != null)
                {
                    // 최대 레벨(5레벨) 특성
                    if (stat.curLevel >= 5)
                    {
                        // 속박(Freeze) 및 슬로우 광역 적용
                        // 적이 죽을 때 터지는 '얼음 파편(Ice Shatter)' 예약 마커 부여
                        // (파편 데미지는 기본 데미지의 40% 등으로 기획에 맞게 조절하세요)
                        nearbyMeteor.Status.AddIcePuddleMark(stat.damage.TotalValue, 2.0f, stat.slowValue.TotalValue);

                        nearbyMeteor.Status.ApplyFreeze(stat.freezeTime.TotalValue, stat.slowValue.TotalValue, stat.slowTime.TotalValue);
                        
                    }
                    // 1~4레벨 특성
                    else
                    {
                        // 슬로우 광역 적용
                        nearbyMeteor.Status.ApplySlow(stat.slowValue.TotalValue, stat.slowTime.TotalValue);
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