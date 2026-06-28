using System;
using System.Collections.Generic;
using UnityEngine;

public class MeteorStat
{
    public Define.MeteorType type;
    public Sprite Sprite;
    public string Name;
    public bool isExclude;

    public GameObject originalPrefabs;

    public Stat MaxHp = new Stat();
    public Stat MaxSpeed = new Stat();
    public Stat MinSpeed = new Stat();
    public Stat Damage = new Stat();
    public Stat Score = new Stat();
    public Stat Exp = new Stat();
    public Stat auraRadius = new Stat();

    [Header("Phase Settings")]
    public Define.PhaseType minPhase; // 등장하기 시작하는 페이즈 (예: 2)
    public Define.PhaseType maxPhase; // 마지막으로 등장하는 페이즈 (예: 3, 즉 4부터는 안 나옴. 0이면 무한히 나옴)

    [Header("Spawn Chance")]
    public float weight; // 스폰 가중치 (이 값이 높을수록 자주 뽑힘)

    public bool targetChase;

    [Header("Drop Item Settings")]
    public List<DropItemRate> dropTable = new List<DropItemRate>();

    public IMeteorBehavior Behavior; // 이 타입이 공유할 단 하나의 뇌!

    public GameObject sludgePuddle;
    public GameObject magmaPuddle;
    public GameObject fragment;

    [Header("Poison Meteor Setting")]
    public Stat poisonTick = new Stat();
    public Stat poisonDamage = new Stat();
    public Stat poisonRadius = new Stat();

    [Header("Explosion Meteor Setting")]
    public Stat explosionRadius = new Stat();
    public Stat explosionDelay = new Stat();
    public Stat explosionTargetRadius = new Stat();


    public void Init(MeteorStatDataSO data)
    {
        if (data == null) return;
        type = data.Type;
        isExclude = data.isExclude;
        MaxHp.Init(data.MaxHp);
        MaxSpeed.Init(data.MaxSpeed);
        MinSpeed.Init(data.MinSpeed);
        Damage.Init(data.Damage);
        Score.Init(data.Score);
        Exp.Init(data.Exp);

        dropTable = data.dropTable;

        minPhase = data.minPhase;
        maxPhase = data.maxPhase;
        weight = data.weight;

        auraRadius.Init(data.auraRadius);

        originalPrefabs = data.originalPrefabs;

        magmaPuddle = data.magmaPuddle;
        sludgePuddle = data.sludgePuddle;
        fragment = data.fragmentMeteor;
        targetChase = data.targetChase;

        poisonTick.Init(data.poisonTick);
        poisonRadius.Init(data.poisonRadius);
        poisonDamage.Init(data.poisonDamage);

        explosionDelay.Init(data.explosionDelay);
        explosionRadius.Init(data.explosionRadius); 
        explosionTargetRadius.Init(data.explosionTargetRadius); 




        // ... 스탯 초기화 ...
        Behavior = CreateBehavior(data);
    }
    private IMeteorBehavior CreateBehavior(MeteorStatDataSO data)
    {
        if (data == null) return null;

        // 규칙에 맞춰 클래스 이름 찾기 (또는 SO에서 가져오기)
        string className = data.Type.ToString() + "Behavior";

        Type t = Type.GetType(className);

        if (t != null)
        {
            return Activator.CreateInstance(t) as IMeteorBehavior;
        }

        return new NormalMeteorBehavior(); // 못 찾으면 깡통 뇌를 줍니다
    }
}
