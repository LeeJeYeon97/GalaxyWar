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

public abstract class BulletAbilityApplier : IAbilityApplier
{
    // 이 클래스를 상속받는 자식들은 어떤 탄환인지 알려줘야 함
    protected abstract BulletType TargetBulletType { get; }
    
    // 자식들이 공통으로 사용할 스탯 가져오기 프로퍼티
    protected BulletStat Stat => Managers.Stat.GetBulletStat(TargetBulletType);

    // 인터페이스 구현은 여기서 틀을 잡고, 실제 로직은 하위 클래스에서 작성
    public abstract void Apply(AbilityDataSO data, int level);
}

#region 유틸성 능력

// 재장전 개수 증가
public class UpgradeReloadCount : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        // TODO : 플레이어 스탯 만들기
    }
}

// 버스트 모드 활성화
public class ActivateBurstMode : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        Managers.Game._player.stat.enableBurst = true;
        Managers.Event.PostEvent(ActionEvent.EnableBurstMode);
    }
}
// 모든 탄 튕기는 횟수 증가
public class UpgradeBulletBounceCount : IAbilityApplier
{
    public void Apply(AbilityDataSO data, int level)
    {
        throw new System.NotImplementedException();
    }
}
#endregion

#region 기본탄

public abstract class NormalBulletApplier : BulletAbilityApplier
{
    protected override BulletType TargetBulletType => BulletType.NormalBullet;
}


// 기본탄 데미지 증가
public class UpgradeBaseBulletDamage : NormalBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    {
        Stat.damage.AddValue(data.GetValue(level));
    }
}
// 기본탄 속도 증가
public class UpgradeBaseBulletSpeed : NormalBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    {
        Stat.speed.AddValue(data.GetValue(level));
    }
}


#endregion

#region 분열탄

public abstract class SplitBulletApplier : BulletAbilityApplier
{
    protected override BulletType TargetBulletType => BulletType.SplitBullet;
}

// 분열탄 활성화
public class ActivateSplitBullet : SplitBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    {
        Stat.isActivated = true;
    }
}

// 분열탄의 데미지 증가
public class UpgradeSplitBulletDamage : SplitBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    {
        Stat.damage.AddValue(data.GetValue(level));        
    }
}

// 분열탄 분열 개수 증가
public class UpgradeSplitBulletCount : SplitBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    {
        Stat.splitCount.AddValue(data.GetValue(level));
    }
}

// 분열탄 장전 확률 증가
public class UpgradeSplitBulletChance : SplitBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    {
        Stat.chance.AddValue(data.GetValue(level));
    }
}
#endregion

#region 폭발탄

public abstract class ExplosionBulletApplier : BulletAbilityApplier
{
    protected override BulletType TargetBulletType => BulletType.ExplosionBullet;
}

// 폭발탄 활성화
public class ActivateExplosionBullet : ExplosionBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    { 
        Stat.isActivated = true;
    }
}

// 폭발탄 폭발 데미지 증가
public class UpgradeExplosionDamage : ExplosionBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    {
        Stat.damage.AddValue(data.GetValue(level));
    }
}

// 폭발탄 폭발 범위 증가
public class UpgradeExplosionRange : ExplosionBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    {
        Stat.explosionRadius.AddValue(data.GetValue(level));
    }
}

// 폭발탄 장전 확률 증가
public class UpgradeExplosionChance : ExplosionBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    {
        Stat.chance.AddValue(data.GetValue(level));
    }
}
#endregion


#region 번개탄
public abstract class LightningBulletApplier : BulletAbilityApplier
{
    protected override BulletType TargetBulletType => BulletType.LightningBullet;
}


public class ActivateLightningBullet : LightningBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    {
        Stat.isActivated = true;
    }
}
#endregion

#region 관통탄
public abstract class PierceBulletApplier : BulletAbilityApplier
{
    protected override BulletType TargetBulletType => BulletType.PierceBullet;
}

public class ActivatePierceBullet : PierceBulletApplier
{
    public override void Apply(AbilityDataSO data, int level)
    {
        Stat.isActivated = true;
    }
}

#endregion

