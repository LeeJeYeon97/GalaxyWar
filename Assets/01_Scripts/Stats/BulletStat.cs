using System;
using UnityEngine;

[Serializable]
public class BulletStat
{
    public Define.BulletType type;
    public IBulletAbility ability;
    public string name;
    public int level;

    // 기본 공통 스탯
    public Stat speed = new Stat();
    public Stat damage = new Stat();
    public Stat hp = new Stat();
    public Stat chance = new Stat();         // 장전될 확률
    public bool isActivated;    // 현재 탄이 활성화 되었는지 확인하는 변수

    // 폭발탄 스탯
    public Stat explosionRadius = new Stat();
    public Stat explosionDamage = new Stat();

    // 분열탄 스탯
    public Stat splitCount = new Stat();
    public void SettingStat(BulletDataSO data)
    {
        level = 0;
        type = data.type;
        name = data.bulletName;

        speed.Init(data.speed);
        damage.Init(data.damage);
        hp.Init(data.hp);
        chance.Init(data.chance);
        isActivated = data.isActivated;

        // 탄세팅
        switch(data)
        {
            case NormalBulletSO normalData:
                ability = new NormalBulletAbility();
                break;
            case ExplosionBulletSO explosionData:
                ability = new ExplosionBulletAbility();
                explosionRadius.Init(explosionData.baseExplosionRange);
                explosionDamage.Init(explosionData.chance);
                break;
        }
        
    }
}
