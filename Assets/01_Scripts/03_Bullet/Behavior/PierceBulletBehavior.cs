using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PierceBulletBehavior : IBulletBehavior
{

    public void OnHit(BulletController bullet, GameObject target)
    {
        if (bullet == null) return;

        Managers.Sound.Play(Define.SoundID.Sfx_PierceBullet_Hit);
        if (bullet.Stat is PierceBulletStat stat)
        {
            bullet.CurDamage = bullet.CurDamage * stat.pierceDamageDecreaseValue.TotalValue;
        }
    }

    public void OnInit(BulletController bullet)
    {
        bullet.Collider.isTrigger = true;

        if (bullet.Stat is PierceBulletStat stat)
        {
            bullet.currentPierceCount = Mathf.FloorToInt(stat.pierceCount.TotalValue);
        }
    }

    public void OnRelease(BulletController bullet)
    {
        bullet.Collider.isTrigger = false;
    }

    public void OnShot(BulletController bullet)
    {
    }

    public void OnUpdate(BulletController bullet)
    {
    }
}