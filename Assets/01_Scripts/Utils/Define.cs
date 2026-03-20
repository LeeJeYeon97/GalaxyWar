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
    public enum SoundID
    {
        None,
        Bgm_Lobby,
        Bgm_Game,
        Sfx_UIButtonClick,
        Sfx_PlayerShot,
        Sfx_NormalBulletHit,
        Sfx_Explosion,
        Sfx_Lightning,
        Sfx_Reloading,
        Sfx_Levelup,
        Sfx_AbilityCardPick,
        Sfx_PlayerHit,
        Sfx_PlayerDie,
        Sfx_PlayerShieldHit,
        
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
        Ready,
        Playing,
        Pause,
        Resume,
        GameOver,
    }
    public enum PlayerState
    {
        Idle,
        Playing,
        Die,
    }
    public enum AbilityType
    {
        Unknown = 0,

        UpgradePlayerHp = 1,                 // 플레이어 체력 증가
        UpgradePlayerSpeed = 2,              // 플레이어 이동속도 증가
        UpgradeReloadCount = 3,              // 재장전 개수 증가
        UpgradeBulletBounceCount = 4,        // 모든탄 튕기는 횟수 증가
        UpgradeBulletSpeed = 5,              // 모든 탄 스피드 증가
        UpgradeReloadTime = 6,               // 리로드 시간 감소
        UpgradeShotTime = 7,                 // 발사 시간 감소
        ActivatePlayerShield = 8,            // 쉴드 활성화
        UpgradeShieldCount = 9,              // 쉴드 갯수 증가

        UpgradeBaseBulletDamage = 10,        // 기본탄 데미지 증가
        
        ActivateSplitBullet = 20,            // 분열탄 활성화
        UpgradeSplitBulletCount = 22,        // 분열탄 갯수 강화
        UpgradeSplitBulletChance = 23,       // 분열탄 확률 강화

        ActivateExplosionBullet = 30,        // 폭발탄 활성화
        UpgradeExplosionDamage = 31,         // 폭발탄 데미지 증가
        UpgradeExplosionRange = 32,          // 폭발탄 범위 증가
        UpgradeExplosionChance = 33,         // 폭발탄 발동 확률 증가
        
        ActivateLightningBullet = 40,        // 번개탄 활성화
        UpgradeLigthningCount = 41,          // 번개탄 전이 횟수 증가
        UpgradeLigthningDamage = 42,         // 번개탄 번개 데미지 증가
        UpgradeLightningRange = 43,          // 번개탄 전이 범위 증가
        UpgradeLightningChance = 44,         // 번개탄 리로드 확률 증가

        ActivatePierceBullet = 50,           // 관통탄 활성화
        UpgradePierceCount = 51,             // 관통횟수 증가
        UpgradePierceDamage = 52,            // 관통 데미지 증가

        ActivateBurstMode = 60,              // 버스트 모드 활성화
        UpgradeBurstModeTime = 61,           // 버스트 모드 시간 감소
    }
    public enum AbilityTargetType
    {
        Unknown = 0,
        Player = 1,
        Meteor = 2,
        Bullet = 3,
    }
    public enum StatType
    {
        Damage,
        Speed,
        Hp,
    }
    public enum BulletType
    {
        NormalBullet = 0,       // 기본탄
        IceBullet = 1,          // 이속저하 얼음탄
        ExplosionBullet = 2,    // 폭발탄
        LightningBullet = 3,    // 번개탄
        PierceBullet = 4,       // 관통탄
        BurstBullet = 5,        // 버스트모드 불릿
        FireBullet = 6,
        PoisonBullet = 7,
        HommingBullet = 8,
    }
    public enum MeteorType
    {
        NormalMeteor = 0,       // 기본 메테오
        CometMeteor = 1,        // 속도빠른 메테오
        IronMeteor = 2,         // 몸빵 메테오
        FractureMeteor = 3,     // 분열하는 메테오
        MagmaMeteor = 4,        // 마그마 메테오(지나간 자리 화염장판 생성)
        GoldenMeteor = 5,       // 아이템 주는 보너스 메테오
        SludgeMeteor = 6,       // 이속 저하 장판까는 메테오(파괴 됐을 때)
        ClusterMeteor = 7,      // 가시 폭발 메테오 -> 파괴 시 가시나 돌 발사
        GravityMeteor = 8 ,     // 블랙홀 메테오
        FragmentMeteor = 9,     // 분열하는 메테오에서 나오는 파편
        AuraBuffMeteor = 10,    // 메테오들한테 장판 버프 주는 메테오
    }
    public enum ItemType
    {
        RecoveryHp,             // HP 회복
        RecoveryBurst,          // 버스트 게이지 회복
    }
    public enum DebuffType
    {
        Slow,
    }
    public enum PhaseType
    {
        Phase1, 
        Phase2, 
        Phase3, 
        Phase4, 
        Phase5
    }
    #region 이벤트(Action) 관련
    // 이벤트 발신용 데이터
    public enum ActionEvent
    {
        PlayerStatusChanged,        // 
        EnableBurstMode,            // 플레이어 버스트 활성화
        ExpChanged,                 // 경험치
        ScoreChanged,               // 점수
        LevelUp,                    // 레벨업
        ReloadStart,                // 리로딩 시작
        ReloadEnd,                  // 리로딩 끝
        
    }
    public struct PlayerStatusEvent
    {
        public float hp;
        public float maxHp;
        public float shield;
        public float maxShield;
        public float burst;
        public float maxBurst;
    }
    
    #endregion
}

