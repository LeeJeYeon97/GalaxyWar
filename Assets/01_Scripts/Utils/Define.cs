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
        Unknown,
        UpgradeBaseBulletDamage,        // 기본탄 데미지 증가
        UpgradeBaseBulletHp,            // 기본탄 튕기는 횟수 증가
        UpgradeBaseBulletCount,         // 기본탄 발사 개수 증가

        ActivateSplitBullet,            // 분열탄 활성화
        UpgradeSplitBulletDamage,       // 분열탄 데미지 강화
        UpgradeSplitBulletCount,        // 분열탄 갯수 강화
        UpgradeSplitBulletChance,       // 분열탄 확률 강화

        ActivateExplosionBullet,        // 폭발탄 활성화
        UpgradeExplosionDamage,         // 폭발탄 범위 데미지 증가
        UpgradeExplosionRange,          // 폭발탄 범위 증가
        UpgradeExplosionChance,         // 폭발탄 발동 확률 증가



    }
    public enum BulletType
    {
        NormalBullet,
        SplitBullet,
        ExplosionBullet,
    }
}

