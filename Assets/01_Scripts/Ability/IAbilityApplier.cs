using UnityEngine;
using static Define;


/// <summary>
/// 능력치 카드 얻었을 때 실행할 함수 정의
/// 1. 클래스 이름은 Define.cs파일의 AbilityType과 동일하기 선언할 것
/// </summary>

public interface IAbilityApplier
{
    // 실제로 스탯이나 로직을 적용하는 함수
    // 업그레이드 한 후의 레벨 받기
    void Apply(AbilityDataSO data, int level);
}

#region 기본탄
// 기본탄 데미지 증가
public class UpgradeBaseBulletDamage : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        BulletStat stat = Managers.Stat.GetBulletStat(BulletType.NormalBullet);
        stat.damage.AddValue(data.GetValue(level));
        Debug.Log($"[{data.abilityname}] 강화! Lv.{level}, 총합: {stat.damage.TotalValue}");
    }
}
// 기본탄 속도 증가
public class UpgradeBaseBulletSpeed : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        BulletStat stat = Managers.Stat.GetBulletStat(BulletType.NormalBullet);
        stat.speed.AddValue(data.GetValue(level));
        Debug.Log($"[{data.abilityname}] 강화! Lv.{level}");
    }
}
#endregion

#region 유틸성 능력

// 재장전 개수 증가
public class UpgradeReloadCount : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {

    }
}
#endregion

#region 분열탄

// 분열탄 활성화
public class ActivateSplitBullet : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        BulletStat stat = Managers.Stat.GetBulletStat(BulletType.SplitBullet);
        stat.isActivated = true;
    }
}

// 분열탄 각 탄의 데미지 증가
public class UpgradeSplitBulletDamage : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        BulletStat stat = Managers.Stat.GetBulletStat(BulletType.SplitBullet);
        stat.isActivated = true;
    }
}

// 분열탄 분열 개수 증가
public class UpgradeSplitBulletCount : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        BulletStat stat = Managers.Stat.GetBulletStat(BulletType.SplitBullet);
        stat.isActivated = true;
    }
}

// 분열탄 장전 확률 증가
public class UpgradeSplitBulletChance : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        BulletStat stat = Managers.Stat.GetBulletStat(BulletType.SplitBullet);
        stat.isActivated = true;
    }
}
#endregion

#region 폭발탄
// 폭발탄 활성화
public class ActivateExplosionBullet : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        BulletStat stat = Managers.Stat.GetBulletStat(BulletType.ExplosionBullet);
        stat.isActivated = true;
    }
}

// 폭발탄 폭발 데미지 증가
public class UpgradeExplosionDamage : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        BulletStat stat = Managers.Stat.GetBulletStat(BulletType.ExplosionBullet);
        stat.isActivated = true;
    }
}

// 폭발탄 폭발 범위 증가
public class UpgradeExplosionRange : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        BulletStat stat = Managers.Stat.GetBulletStat(BulletType.ExplosionBullet);
        stat.isActivated = true;
    }
}

// 폭발탄 장전 확률 증가
public class UpgradeExplosionChance : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        BulletStat stat = Managers.Stat.GetBulletStat(BulletType.ExplosionBullet);
        stat.isActivated = true;
    }
}
#endregion