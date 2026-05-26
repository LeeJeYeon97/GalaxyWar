using JetBrains.Annotations;
using System;
using System.Reflection;
using UnityEngine;

[Serializable]
public class PlayerStat
{
    public Stat speed = new Stat();

    public Stat maxHp = new Stat();
    public Stat maxDefenceCount = new Stat();
    public Stat shieldChargeTime = new Stat();
    public float maxDefenceGuage;

    public Stat reloadCount = new Stat();
    public Stat reloadTime = new Stat();

    public Stat shotRange = new Stat();
    public Stat shotTime = new Stat();

    public bool enableBurst = false;
    public Stat maxBurstGuage = new Stat();
    public Stat maxBurstFullChargeTime = new Stat();


    public Stat criticalChance = new Stat();
    public Stat criticalDamageRate = new Stat();

    public Stat itemGetRange = new Stat();

    public float hitCooldown;       // 피격 딜레이

    [Header("멀티샷관련")]
    public bool isMultiShotEnabled; // 멀티샷(분열) 능력 획득 여부
    public Stat multiShotCount = new Stat();
    public Stat multiShotChance = new Stat();
    public float multiShotAngle;

    [Header("유도탄 관련")]
    public bool isHomingShotEnabled;

    // 현재 적용받고 있는 버스트 모드 상세 스탯 기억용
    public BurstModeStat currentBurstStat;
    public void SetStat(PlayerStatDataSO data)
    {
        if(data == null)
        {
            return;
        }

        speed.Init(data.statData.speed);
        maxHp.Init(data.statData.maxHp);
        maxDefenceCount.Init(data.statData.maxDefenceCount);
        reloadCount.Init(data.statData.reloadCount);
        reloadTime.Init(data.statData.reloadTime);
        shotRange.Init(data.statData.shotRange);
        shotTime.Init(data.statData.shotTime);
        maxDefenceGuage = data.statData.maxDefenceGuage;

        shieldChargeTime.Init(data.statData.shieldChargeTime);
        enableBurst = data.statData.isBurstModeEnabled;
        maxBurstGuage.Init(data.statData.maxBurstGuage);
        maxBurstFullChargeTime.Init(data.statData.maxBurstFullChargeTime);
        hitCooldown = data.statData.hitCooldown;
        itemGetRange.Init(data.statData.itemGetRange);

        isMultiShotEnabled = data.statData.isMultiShotEnabled;
        multiShotCount.Init(data.statData.multiShotCount);
        multiShotAngle = data.statData.multiShotAngle;
        multiShotChance.Init(data.statData.multiShotChance);

        isHomingShotEnabled = data.statData.isHomingShotEnabled;

    }
    //  [추가] 버스트 스탯 적용 함수
    public void ApplyBurstBuff()
    {
        speed.AddMultiplier(currentBurstStat.speed);
        reloadTime.SetForceZero(true);
        shotTime.SetForceValue(true, 0.1f);

        if (Managers.Ability.GetCurrentLevel(Define.AbilityType.Passive_PlayerCritical) > 0)
        {
            criticalDamageRate.AddMultiplier(currentBurstStat.criticalDamageRate);
            criticalChance.SetForceValue(true, 100.0f);
        }
        if(Managers.Ability.GetCurrentLevel(Define.AbilityType.Passive_SplitBullet) > 0)
        {
            multiShotChance.SetForceValue(true, 100.0f);
        }
    }

    //  [추가] 버스트 스탯 해제 함수
    public void RemoveBurstBuff()
    {
        speed.SubMultiplier(currentBurstStat.speed);
        criticalDamageRate.SubMultiplier(currentBurstStat.criticalDamageRate);

        reloadTime.SetForceZero(false);
        shotTime.SetForceValue(false);
        criticalChance.SetForceValue(false);
        multiShotChance.SetForceValue(false);
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
