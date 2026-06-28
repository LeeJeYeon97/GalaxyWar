using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class LightningBulletBehavior : IBulletBehavior
{
 

    public void OnHit(BulletController bullet, GameObject target, BaseBulletStat activeStat)
    {
        if (target == null) return;

        MeteorController meteor = target.GetComponent<MeteorController>();
        if (meteor == null) return;

        Vector3 hitPos = meteor.transform.position;

        if (activeStat is LightningBulletStat stat)
        {
            
            // 1. 번개 전담 객체(LightningChain)를 풀에서 꺼냅니다.
            // (리소스 경로는 유저님 프로젝트에 맞게 수정해주세요! 또는 param.stat 안에 프리팹을 넣어두면 가장 좋습니다.)

            GameObject chainGo = Managers.Resource.Instantiate(stat.ligthningChainObject);
            Managers.Sound.Play(Define.SoundID.Sfx_Lightning_Hit);
            if (chainGo != null && chainGo.TryGetComponent<LightningChain>(out var chain))
            {
                // 2. 알아서 전이하면서 데미지 주라고 파라미터를 꽉꽉 채워줍니다.
                chain.Init(
                    startPos: hitPos,
                    firstTarget: target,
                    damage: bullet.CurDamage * (stat.lightningDamageValue.TotalValue/ 100f),
                    range: stat.lightningRange.TotalValue,
                    count: Mathf.FloorToInt(stat.lightningCount.TotalValue),
                    stat.curLevel >= 5 ? true : false
                );
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
