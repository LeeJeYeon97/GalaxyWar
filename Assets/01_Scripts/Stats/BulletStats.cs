using System;
using UnityEngine;

[Serializable]
public abstract class BaseBulletStat
{
    public Define.BulletType type;
    public IBulletBehavior behavior;
    public GameObject originalPrefabs;
    public Sprite CardIcon;
    public Sprite hudIcon;

    public Stat speed = new Stat();
    public Stat damage = new Stat();
    public Stat bounceCount = new Stat();
    public Stat chance = new Stat();
    public bool isReload;

    public int curLevel;


    // 가상 함수(virtual): 자식들이 이 함수를 물려받아서 자기 스탯을 추가로 세팅할 수 있게 합니다.
    public virtual void Init(BulletStatDataSO data)
    {
        type = data.type;
        originalPrefabs = data.originalPrefab;

        CardIcon = data.stats.CardIcon;
        hudIcon = data.stats.hudIcon;

        speed.Init(data.stats.speed);
        damage.Init(data.stats.damage);
        bounceCount.Init(data.stats.bounceCount);
        chance.Init(data.stats.chance);
        curLevel = (data.type == Define.BulletType.NormalBullet) ? 1 : 0;
        isReload = data.isReload;

        behavior = CreateAbility(data);
        
    }

    // (기존 CreateAbility 로직 그대로 유지)
    protected IBulletBehavior CreateAbility(BulletStatDataSO data)
    {
        if (data == null) return null;
        string className = data.type.ToString() + "Behavior";
        Type t = Type.GetType(className);
        if (t != null)
        {
            return Activator.CreateInstance(t) as IBulletBehavior;
        }
        Debug.LogError($"[BulletStat] {className} Behavior를 찾을 수 없습니다!");
        return null;
    }
}
[Serializable]
public class NormalBulletStat : BaseBulletStat
{
    public override void Init(BulletStatDataSO data)
    {
        // 1. 일단 부모(Base)한테 기본 공통 스탯 세팅을 맡깁니다.
        base.Init(data);

        if (data is NormalBulletStatDataSO da)
        {
            //
        }
    }
}
[Serializable]
public class ExplosionBulletStat : BaseBulletStat
{
    public Stat explosionRange = new Stat();
    public Stat explosionDamage = new Stat();
    public override void Init(BulletStatDataSO data)
    {
        // 1. 일단 부모(Base)한테 기본 공통 스탯 세팅을 맡깁니다.
        base.Init(data);

        if (data is ExplosionBulletStatDataSO explosionData)
        {
            // 3. 화염탄 전용 스탯을 세팅합니다!
            explosionRange.Init(explosionData.explosionStat.explosionRange);
            explosionDamage.Init(explosionData.explosionStat.explosionDamageValue);
        }
    }
}
[Serializable]
public class LightningBulletStat : BaseBulletStat
{
    [Header("Lighting Stat Settings")]
    public Stat lightningDamageValue = new Stat();
    public Stat lightningRange = new Stat();
    public Stat lightningCount = new Stat();

    public GameObject ligthningChainObject;
    public override void Init(BulletStatDataSO data)
    {
        // 1. 일단 부모(Base)한테 기본 공통 스탯 세팅을 맡깁니다.
        base.Init(data);

        if (data is LightningBulletStatDataSO da)
        {
            lightningDamageValue.Init(da.lightningStat.lightningDamageValue);
            lightningRange.Init(da.lightningRange);
            lightningCount.Init(da.lightningStat.lightningCount);
            ligthningChainObject = da.lightningChain;
        }
    }
}
[Serializable]
public class FireBulletStat : BaseBulletStat //상속!
{
    public Stat fireRemainTime = new Stat();
    public Stat fireDamageValue = new Stat();
    public Stat fireZoneDestroyTime = new Stat();
    // 부모의 세팅 함수를 덮어씌웁니다(override).
    public override void Init(BulletStatDataSO data)
    {
        // 1. 일단 부모(Base)한테 기본 공통 스탯 세팅을 맡깁니다.
        base.Init(data);

        // 2. 넘어온 data를 FireBulletStatDataSO로 변환(DownCasting)합니다.
        if (data is FireBulletStatDataSO fireData)
        {
            // 3. 화염탄 전용 스탯을 세팅합니다!
            fireRemainTime.Init(fireData.fireStat.fireRemainTime);
            fireDamageValue.Init(fireData.fireStat.fireDamageValue);
            fireZoneDestroyTime.Init(fireData.fireStat.fireZoneDestroyTime);
        }
    }
}
[Serializable]
public class IceBulletStat : BaseBulletStat
{
    [Header("IceBullet Stat Settings")]
    public Stat slowValue = new Stat();
    public Stat slowTime = new Stat();
    public Stat freezeTime = new Stat();
    
    public override void Init(BulletStatDataSO data)
    {
        // 1. 일단 부모(Base)한테 기본 공통 스탯 세팅을 맡깁니다.
        base.Init(data);

        if (data is IceBulletStatDataSO da)
        {
            // 3. 전용 스탯을 세팅합니다!
            slowValue.Init(da.iceBulletStat.slowValue);
            slowTime.Init(da.iceBulletStat.slowTime);
            freezeTime.Init(da.iceBulletStat.freezeTime);
        }
    }
}
[Serializable]
public class PierceBulletStat : BaseBulletStat
{
    [Header("PierceBullet Stat Settings")]
    public Stat pierceCount = new Stat();
    public Stat pierceDamageDecreaseValue = new Stat();

    public override void Init(BulletStatDataSO data)
    {
        // 1. 일단 부모(Base)한테 기본 공통 스탯 세팅을 맡깁니다.
        base.Init(data);

        if (data is PierceBulletStatDataSO da)
        {
            pierceCount.Init(da.pierceBulletStat.pierceCount);
            pierceDamageDecreaseValue.Init(da.pierceBulletStat.pierceDamageDecreaseValue);
        }
    }
}
[Serializable]
public class HomingBulletStat : BaseBulletStat
{
    public float turnSpeed;
    public Stat homingShotDelay = new Stat();
    public override void Init(BulletStatDataSO data)
    {
        // 1. 일단 부모(Base)한테 기본 공통 스탯 세팅을 맡깁니다.
        base.Init(data);

        if (data is HomingBulletStatDataSO da)
        {
            homingShotDelay.Init(da.homingBulletStat.homingShotDelay);
            turnSpeed = da.trunSpeed;
        }
    }
}

[Serializable]
public class BurstBulletStat : BaseBulletStat
{
    public override void Init(BulletStatDataSO data)
    {
        // 1. 일단 부모(Base)한테 기본 공통 스탯 세팅을 맡깁니다.
        base.Init(data);

        if (data is BurstBulletStatDataSO da)
        {
           //
        }
    }
}