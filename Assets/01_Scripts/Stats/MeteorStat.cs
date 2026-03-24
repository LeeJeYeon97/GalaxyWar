using System;
using UnityEngine;

public class MeteorStat
{
    public Define.MeteorType type;
    public Sprite Sprite;
    public string Name;
    public bool isExclude;

    public Stat MaxHp = new Stat();
    public Stat MaxSpeed = new Stat();
    public Stat MinSpeed = new Stat();
    public Stat Damage = new Stat();
    public Stat Score = new Stat();
    public Stat Exp = new Stat();

    public Stat auraRadius = new Stat();

    public Define.PhaseType spawnPhase;

    public IMeteorBehavior Behavior; // 이 타입이 공유할 단 하나의 뇌!

    public void Init(MeteorStatDataSO data)
    {
        if (data == null) return;
        type = data.Type;
        Sprite = data.Sprite;
        Name = data.Name;
        isExclude = data.isExclude;
        MaxHp.Init(data.MaxHp);
        MaxSpeed.Init(data.MaxSpeed);
        MinSpeed.Init(data.MinSpeed);
        Damage.Init(data.Damage);

        Score.Init(data.Score);
        Exp.Init(data.Exp);

        spawnPhase = data.spawnPhase;
        auraRadius.Init(data.auraRadius);

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
