using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class NormalBulletBehavior : IBulletBehavior
{

    public void OnHit(BulletController bullet, GameObject target, BaseBulletStat activeStat)
    {
        if (bullet == null) return;

        if (activeStat is NormalBulletStat stat)
        {
            if (bullet.currentPierceCount > 0)
            {
                float decreasePercent = stat.pierceDamageDecreaseValue.TotalValue;

                float multiplier = (100f - decreasePercent) / 100f;
                multiplier = Mathf.Clamp(multiplier, 0.8f, 1f);

                bullet.CurDamage = bullet.CurDamage * multiplier;

                if (bullet.CurDamage < 1f)
                {
                    bullet.CurDamage = 1f;
                }
            }
        }
    }

    public void OnInit(BulletController bullet, BaseBulletStat activeStat)
    {
        if (activeStat is NormalBulletStat stat)
        {
            bullet.currentPierceCount = Mathf.FloorToInt(stat.pierceCount.TotalValue);
        }
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
