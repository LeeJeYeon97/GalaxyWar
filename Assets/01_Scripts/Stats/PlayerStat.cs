using System;
using System.Reflection;
using UnityEngine;

[Serializable]
public class PlayerStat
{
    public Stat speed = new Stat();

    public Stat maxHp = new Stat();
    public Stat maxDefence = new Stat();

    public Stat reloadCount = new Stat();
    public Stat reloadTime = new Stat();

    public Stat shotRange = new Stat();
    public Stat shotTime = new Stat();

    public bool enableBurst = false;
    public Stat maxBurstGuage = new Stat();
    public Stat maxBurstFullChargeTime = new Stat();

    public float hitCooldown;       // 피격 딜레이

    [Header("멀티샷관련")]
    public bool isMultiShotEnabled; // 멀티샷(분열) 능력 획득 여부
    public Stat multiShotCount = new Stat();
    public Stat multiShotChance = new Stat();
    public float multiShotAngle;

    public void SetStat(PlayerStatDataSO data)
    {
        if(data == null)
        {
            return;
        }

        speed.Init(data.speed);
        maxHp.Init(data.maxHp);
        maxDefence.Init(data.maxDefence);
        reloadCount.Init(data.reloadCount);
        reloadTime.Init(data.reloadTime);
        shotRange.Init(data.shotRange);
        shotTime.Init(data.shotTime);

        enableBurst = false;
        maxBurstGuage.Init(data.maxBurstGuage);
        maxBurstFullChargeTime.Init(data.maxBurstFullChargeTime);
        hitCooldown = data.hitCooldown;

        isMultiShotEnabled = false;
        multiShotCount.Init(data.multiShotCount);
        multiShotAngle = data.multiShotAngle;
        multiShotChance.Init(data.multiShotChance);
    }
    private void AutoInitStats()
    {
        // 1. 내 몸(PlayerStat)안에 있는 모든 변수(Field) 목록을 가져옵니다.
        FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            // 2. 만약 변수의 타입이 'Stat' 클래스라면?
            if (field.FieldType == typeof(Stat))
            {
                // 3. 그 변수가 비어있는지(null) 확인하고, 비어있다면 새로 생성(new)해서 넣어줍니다!
                if (field.GetValue(this) == null)
                {
                    field.SetValue(this, new Stat());
                }
            }
        }
    }
}
