using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;


public class BurstBulletBehavior : IBulletBehavior
{
    public void OnHit(BulletController bullet, GameObject target, BaseBulletStat activeStat)
    {
        if (target == null) return;

        // 1. 조건에 맞는 스탯들만 담아둘 '빈 바구니(리스트)'를 먼저 만듭니다.
        List<BaseBulletStat> ownedStats = new List<BaseBulletStat>();

        // 2. 딕셔너리에 있는 모든 스탯을 하나씩 꺼내서 검사합니다.
        foreach (BaseBulletStat stat in Managers.Stat.bulletStatDict.Values)
        {
            // 3. 레벨이 0보다 큰(배운) 스킬만 바구니에 담습니다.
            if (stat.curLevel <= 0)
            {
                continue;
            }

            if (stat.type == BulletType.BurstBullet ||
                stat.type == BulletType.PoisonBullet ||
                stat.type == BulletType.HomingBullet ||
                stat.type == BulletType.NormalBullet)
            {
                continue;
            }

            ownedStats.Add(stat);
        }

        // 3. 보유한 모든 능력을 순회하면서 펑펑 터트립니다!
        foreach (BaseBulletStat stat in ownedStats)
        {
            // 해당 능력에 맞는 Behavior를 가져옵니다.
            // (팩토리 클래스나 매니저에서 타입에 맞는 Behavior를 꺼내오는 함수가 있다고 가정합니다)
            IBulletBehavior behavior = GetBehaviorByType(stat.type);

            if (behavior != null)
            {
                behavior.OnHit(bullet, target, stat);
            }
        }
        // (선택) 버스트 전용의 거대한 타격 사운드나 이펙트를 여기서 하나 터트려주면 더욱 좋습니다!
        //Managers.Sound.Play(Define.SoundID.Sfx_Burst_Hit);
    }

    public void OnInit(BulletController bullet, BaseBulletStat activeStat) 
    {
        if (activeStat == null) return;

        // 1. 조건에 맞는 스탯들만 담아둘 '빈 바구니(리스트)'를 먼저 만듭니다.
        List<BaseBulletStat> ownedStats = new List<BaseBulletStat>();

        // 2. 딕셔너리에 있는 모든 스탯을 하나씩 꺼내서 검사합니다.
        foreach (BaseBulletStat stat in Managers.Stat.bulletStatDict.Values)
        {
            // 3. 레벨이 0보다 큰(배운) 스킬만 바구니에 담습니다.
            if (stat.curLevel <= 0)
            {
                continue;
            }

            if (stat.type == BulletType.BurstBullet ||
                stat.type == BulletType.PoisonBullet ||
                stat.type == BulletType.HomingBullet ||
                stat.type == BulletType.NormalBullet)
            {
                continue;
            }

            ownedStats.Add(stat);
        }

        // 3. 보유한 모든 능력을 순회하면서 펑펑 터트립니다!
        foreach (BaseBulletStat stat in ownedStats)
        {
            // 해당 능력에 맞는 Behavior를 가져옵니다.
            // (팩토리 클래스나 매니저에서 타입에 맞는 Behavior를 꺼내오는 함수가 있다고 가정합니다)
            IBulletBehavior behavior = GetBehaviorByType(stat.type);

            if (behavior != null)
            {
                behavior.OnInit(bullet,stat);
            }
        }
    }
    public void OnRelease(BulletController bullet) { }
    public void OnShot(BulletController bullet) { }
    public void OnUpdate(BulletController bullet) { }

    private IBulletBehavior GetBehaviorByType(BulletType type)
    {
        switch (type)
        {
            case BulletType.ExplosionBullet: return new ExplosionBulletBehavior();
            case BulletType.LightningBullet: return new LightningBulletBehavior();
            case BulletType.IceBullet: return new IceBulletBehavior();
            case BulletType.FireBullet: return new FireBulletBehavior();
            case BulletType.PierceBullet: return new PierceBulletBehavior();
            default: return null;
        }
    }
}

