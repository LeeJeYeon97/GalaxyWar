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
        bullet.Collider.isTrigger = true;
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