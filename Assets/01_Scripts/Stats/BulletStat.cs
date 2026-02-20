using System;
using UnityEngine;

[Serializable]
public class BulletStat
{
    public Define.BulletType type;
    public Define.Pool poolType;
    public IBulletAbility ability;
    public string name;
    public int level;

    // 기본 공통 스탯
    public Stat speed = new Stat();
    public Stat damage = new Stat();
    public Stat bounceCount = new Stat();
    public Stat chance = new Stat();         // 장전될 확률
    public bool isActivated;    // 현재 탄이 활성화 되었는지 확인하는 변수

    // 폭발탄 스탯
    public Stat explosionRadius = new Stat();
    
    public Stat splitCount = new Stat();

    public Stat lightningRange = new Stat();
    public Stat lightningCount = new Stat();

    public Stat pierceCount = new Stat();
    public void SettingStat(BulletStatDataSO data)
    {
        level = 0;
        type = data.type;
        
        name = data.bulletName;
        poolType = data.poolType;
        speed.Init(data.speed);
        damage.Init(data.damage);
        bounceCount.Init(data.bounceCount);

        chance.Init(data.chance);
        isActivated = data.isActivated;

        // 폭발탄 세팅
        explosionRadius.Init(data.baseExplosionRange);

        // 분열탄
        splitCount.Init(data.baseSplitCount);

        // 번개탄
        lightningRange.Init(data.lightningRange);
        lightningCount.Init(data.lightningCount);

        // 관통탄
        pierceCount.Init(data.pierceCount);

        // 어빌리티 능력(실행코드) 세팅
        ability = CreateAbility(data);
    }

    private IBulletAbility CreateAbility(BulletStatDataSO data)
    {
        if (data == null) return null;
        
        // 1. Enum 이름을 문자열로 변환 (예: "BulletData")
        string className = data.type.ToString() + "Ability";

        // 2. 현재 어셈블리(내 프로젝트 코드)에서 해당 이름의 클래스 타입을 찾음
        Type t = Type.GetType(className);
        if (t != null)
        {
            // 3. 찾은 타입으로 인스턴스 생성 (new 하는 것과 동일)
            IBulletAbility ability = Activator.CreateInstance(t) as IBulletAbility;
            return ability;
        }
        Debug.LogError($"[BulletStat] {className} ability를 찾을 수 없습니다!");
        return null;
        


        
    }

}
