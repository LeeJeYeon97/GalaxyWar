using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ExplosionBulletBehavior : IBulletBehavior
{

    public void OnHit(BulletController bullet, GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (bullet.Stat is ExplosionBulletStat stat)
        {
            float radius = stat.explosionRange.TotalValue;
            float finalExplosionDmg = stat.damage.TotalValue * stat.explosionDamage.TotalValue;

            Managers.Sound.Play(Define.SoundID.Sfx_Explosion_Hit);

            // --- 실제 범위 데미지 로직 ---
            int layerMask = 1 << LayerMask.NameToLayer("Meteor");
            Collider2D[] colliders = Physics2D.OverlapCircleAll(target.transform.position, radius, layerMask);
            foreach (var col in colliders)
            {
                // 이미 맞은놈 제외
                if (col.gameObject == target) continue;

                MeteorController meteor = col.GetComponent<MeteorController>();
                if (meteor)
                {
                    meteor.OnDamage(finalExplosionDmg);
                }
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
