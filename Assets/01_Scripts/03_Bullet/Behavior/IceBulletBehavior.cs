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
            // 2. 얼음탄 스탯 가져오기 (임시로 수치를 넣었지만, 나중에 Stat 데이터로 빼시면 됩니다!)
            float slowPercent = stat.slowValue.TotalValue;    // 50% 느려짐
            float duration = stat.slowTime.TotalValue;       // 2초 동안 지속
            float freezeChance = stat.freezeChance.TotalValue;   // 완전히 얼어붙을 빙결 확률 (20%)

            bool isFreeze = UnityEngine.Random.Range(0f, 100f) <= 20f; // 20% 확률로 완전 빙결

            if (isFreeze)
            {
                // 스탯에 직접 접근(target.Stat.speed.SetForceZero)하지 않고, 함수를 통해 정중하게 명령만 내립니다.
                meteor.ApplyFreeze(2.0f);
            }
            else
            {
                // 50% 슬로우를 2초간 부여
                meteor.ApplySlow(0.5f, 2.0f);
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