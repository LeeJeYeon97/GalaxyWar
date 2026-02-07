using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    public enum Scene
    {
        Unknown,
        LobbyScene,
        LoginScene,
        GameScene,
    }
    public enum Sound
    {
        Bgm,
        Sfx,
        MaxCount,
    }
    public enum Pool
    {
        NormalBullet,
        ExplosionBullet,
        SplitBullet,
        LightningBullet,

        Meteor,
        ExplosionRangeIndicator,
        NormalBullet_Hit,
        NormalBullet_Flash,

        LightningEffect
    }
    public enum UIEvent
    {
        Click,
        Drag,
    }

    public enum MouseEvent
    {
        Press,
        Click,
    }

    public enum CameraMode
    {
        QuarterView,
    }

    public enum GameState
    {
        Playing,
        Reload,
        Pause,
        Resume,
        GameOver,
    }
    public enum AbilityType
    {
        Unknown = 0,

        UpgradePlayerHp = 1,                // 플레이어 체력 증가
        UpgradePlayerSpeed = 2,             // 플레이어 이동속도 증가
        UpgradeReloadCount = 3,             // 재장전 개수 증가

        UpgradeBaseBulletDamage = 10,        // 기본탄 데미지 증가
        UpgradeBaseBulletSpeed = 11,         // 기본탄 속도 증가
        UpgradeBaseBulletBounceCount = 12,   // 기본탄 튕기는 횟수 증가

        ActivateSplitBullet = 20,            // 분열탄 활성화
        UpgradeSplitBulletDamage = 21,       // 분열탄 데미지 강화
        UpgradeSplitBulletCount = 22,        // 분열탄 갯수 강화
        UpgradeSplitBulletChance = 23,       // 분열탄 확률 강화

        ActivateExplosionBullet = 30,        // 폭발탄 활성화
        UpgradeExplosionDamage = 31,         // 폭발탄 데미지 증가
        UpgradeExplosionRange = 32,          // 폭발탄 범위 증가
        UpgradeExplosionChance = 33,         // 폭발탄 발동 확률 증가
        
        ActivateLightningBullet = 40,        // 번개탄 활성화

    }
    public enum BulletType
    {
        NormalBullet = 0,       // 기본탄
        SplitBullet = 1,        // 분열탄
        ExplosionBullet = 2,    // 폭발탄
        LightningBullet =3,     // 번개탄
        
    }
}

