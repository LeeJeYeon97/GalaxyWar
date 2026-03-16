using UnityEngine;

public class MeteorStat
{
    public Define.MeteorType Type;
    public Sprite Sprite;
    public string Name;
    public bool isExclude;

    public Stat MaxHp = new Stat();
    public Stat MaxSpeed = new Stat();
    public Stat MinSpeed = new Stat();
    public Stat Damage = new Stat();
    public Stat Score = new Stat();
    public Stat Exp = new Stat();
    
    public void Init(MeteorStatDataSO data)
    {
        if (data == null) return;
        Type = data.Type;
        Sprite = data.Sprite;
        Name = data.Name;
        isExclude = data.isExclude;
        MaxHp.Init(data.MaxHp);
        MaxSpeed.Init(data.MaxSpeed);
        MinSpeed.Init(data.MinSpeed);
        Damage.Init(data.Damage);

        Score.Init(data.Score);
        Exp.Init(data.Exp);
    }
}
