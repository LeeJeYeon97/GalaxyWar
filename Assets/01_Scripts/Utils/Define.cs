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
        Bullet,
        Meteor,
        ExplosionRangeIndicator,
        NormalBullet_Hit,
        NormalBullet_Flash,

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

        UpgradeBaseBulletDamage = 1,        // 기본탄 데미지 증가
        UpgradeBaseBulletSpeed = 2,         // 기본탄 속도 증가
        UpgradeReloadCount = 3,             // 재장전 개수 증가

        ActivateSplitBullet = 4,            // 분열탄 활성화
        UpgradeSplitBulletDamage = 5,       // 분열탄 데미지 강화
        UpgradeSplitBulletCount = 6,        // 분열탄 갯수 강화
        UpgradeSplitBulletChance = 7,       // 분열탄 확률 강화

        ActivateExplosionBullet = 8,        // 폭발탄 활성화
        UpgradeExplosionDamage = 9,         // 폭발탄 범위 데미지 증가
        UpgradeExplosionRange = 10,          // 폭발탄 범위 증가
        UpgradeExplosionChance = 11,         // 폭발탄 발동 확률 증가

    }
    public enum BulletType
    {
        NormalBullet = 0,
        SplitBullet = 1,
        ExplosionBullet = 2,
    }
}

