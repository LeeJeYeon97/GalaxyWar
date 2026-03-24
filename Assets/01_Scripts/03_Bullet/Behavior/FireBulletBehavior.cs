using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class FireBulletBehavior : IBulletBehavior
{

    public void OnHit(BulletController bullet, GameObject target)
    {
        if (target == null) return;

        MeteorController meteor = target.GetComponent<MeteorController>();

        if (meteor == null) return;

        if (bullet.Stat is FireBulletStat stat)
        {
            // 이 중괄호 안에서는 stat이 완벽한 FireBulletStat으로 작동합니다.
            float totalBurnDamage = stat.damage.TotalValue * stat.fireDamageValue.TotalValue;
            Debug.Log("FireBullet Test");
            //meteor.ApplyBurn(totalBurnDamage, 3.0f, 0.5f);
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