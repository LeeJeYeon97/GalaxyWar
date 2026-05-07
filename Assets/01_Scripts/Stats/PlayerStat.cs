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

    public Stat maxShield = new Stat();
    public Stat shieldChargeTime = new Stat();

    public Stat criticalChance = new Stat();
    public Stat criticalDamageRate = new Stat();

    public float hitCooldown;       // 피격 딜레이

    [Header("멀티샷관련")]
    public bool isMultiShotEnabled; // 멀티샷(분열) 능력 획득 여부
    public Stat multiShotCount = new Stat();
    public Stat multiShotChance = new Stat();
    public float multiShotAngle;

    [Header("유도탄 관련")]
    public bool isHomingShotEnabled;
    public void SetStat(PlayerStatDataSO data)
    {
        if(data == null)
        {
            return;
        }

        speed.Init(data.statData.speed);
        maxHp.Init(data.statData.maxHp);
        maxDefence.Init(data.statData.maxDefence);
        reloadCount.Init(data.statData.reloadCount);
        reloadTime.Init(data.statData.reloadTime);
        shotRange.Init(data.statData.shotRange);
        shotTime.Init(data.statData.shotTime);
        maxShield.Init(data.statData.maxDefence);

        shieldChargeTime.Init(data.statData.shieldChargeTime);
        enableBurst = data.statData.isBurstModeEnabled;
        maxBurstGuage.Init(data.statData.maxBurstGuage);
        maxBurstFullChargeTime.Init(data.statData.maxBurstFullChargeTime);
        hitCooldown = data.statData.hitCooldown;

        isMultiShotEnabled = data.statData.isMultiShotEnabled;
        multiShotCount.Init(data.statData.multiShotCount);
        multiShotAngle = data.statData.multiShotAngle;
        multiShotChance.Init(data.statData.multiShotChance);

        isHomingShotEnabled = data.statData.isHomingShotEnabled;

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
