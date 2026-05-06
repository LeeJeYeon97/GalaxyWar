using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class IceBulletBehavior : IBulletBehavior
{

    public void OnHit(BulletController bullet, GameObject target)
    {
        if (target == null)
        {
            return;
        }

        MeteorController meteor = target.GetComponent<MeteorController>();
        if (meteor == null)
        {
            return;
        }

        if(bullet.Stat is IceBulletStat stat)
        {
            Managers.Sound.Play(Define.SoundID.Sfx_IceBullet_Hit);

            // 최대 레벨이면
            if (stat.curLevel >= 5)
            {
                // 스탯에 직접 접근(target.Stat.speed.SetForceZero)하지 않고, 함수를 통해 정중하게 명령만 내립니다.
                meteor.Status.ApplyFreeze(stat.freezeTime.TotalValue, stat.slowValue.TotalValue, stat.slowTime.TotalValue);
            }
            else
            {
                // 50% 슬로우를 2초간 부여
                meteor.Status.ApplySlow(stat.slowValue.TotalValue, stat.slowTime.TotalValue);
            }
        }
    }

    public void OnInit(BulletController bullet)
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