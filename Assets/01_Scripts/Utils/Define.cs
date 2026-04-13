using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    // 서버 키
    public const string k_PlayerDataKey = "PLAYER_DATA";
    public const string k_PlayerNameKey = "PLAYER_NAME";

    public const string k_GoldCurrencyKey = "GOLD";
    public const string k_HealthPotionKey = "HEALTH_POTION";
    public const string k_testEquipKey = "TEST_EQUIP";

    public const string k_goldPurchase100Id = "GOLD_PURCHASE_100";
    public const string k_HealthPotionPurchaseId = "HEALTH_POTION_VIRTUAL_PURCHASE";
    public const string k_testEquipPurchaseId = "VP_TEST_EQUIP";

    public const string placementShopGoldAd = "Shop_Gold";

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
        // ==========================================
        // [1. 글로벌 패시브] : 모든 총알과 플레이어에게 공통 적용
        // ==========================================
        // 플레이어
        Passive_MaxHpUp = 1,                // 플레이어 체력 증가
        Passive_PlayerSpeedUp = 2,          // 플레이어 이동속도 증가
        Passive_ReloadCountUp = 3,          // 플레이어 재장전 개수 증가
        Passive_ReloadTimeDown = 4,         // 리로드 시간 감소
        Passive_ShotTimeDown = 5,           // 발사 딜레이 감소
        Passive_BurstMode = 6,              // 버스트 모드 활성화 및 충전 시간 감소
        // TODO : 플레이어 쉴드

        // 모든 총알
        Passive_AllBulletBounceCountUp = 10,// 모든 총알의 바운스(튕기는) 횟수 증가
        Passive_AllBulletSpeed = 11,        // 모든 총알의 속도 증가
        
        // 특수 기능
        Passive_SplitBullet = 20,           // 분열 기능 업그레이드

        // ==========================================
        // [2. 액티브 무기] : 획득 시 Lv.1, 중복 획득 시 레벨업 (최대 Lv.5)
        // ==========================================
        Weapon_LaserBeam = 100,                // 레이저탄 : TODO
        Weapon_ExplosionBullet = 101,         // 폭발탄 (광역 데미지)
        Weapon_LightningBullet = 102,         // 번개탄 (체인 라이트닝)
        Weapon_PierceBullet = 103,            // 관통탄 (직선 관통)
        Weapon_IceBullet = 104,               // 얼음탄 (슬로우 및 빙결 CC)
        Weapon_HomingBullet = 105,            // 유도탄 (적 추적)
        Weapon_FireBullet = 106,              // 화염탄 (장판 및 화상 DoT)

        // 신규 무기 자리
        Weapon_BlackHoleBullet = 107,         // 블랙홀탄
        Weapon_PoisonBullet = 108,            // 맹독탄
        
        // ==========================================
        // [2. 액티브 무기] : 획득 시 Lv.1, 중복 획득 시 레벨업 (최대 Lv.5)
        // ==========================================

        // 분열탄(플레이어)
        //ActivateSplitBullet = 20,            // 분열탄 활성화
        //UpgradeSplitBulletCount = 22,        // 분열탄 갯수 강화
        //UpgradeSplitBulletChance = 23,       // 분열탄 확률 강화

        //// 폭발탄
        //ActivateExplosionBullet = 30,        // 폭발탄 활성화
        //UpgradeExplosionDamage = 31,         // 폭발탄 데미지 증가
        //UpgradeExplosionRange = 32,          // 폭발탄 범위 증가
        //UpgradeExplosionChance = 33,         // 폭발탄 확률 증가

        //// 번개탄
        //ActivateLightningBullet = 40,        // 번개탄 활성화
        //UpgradeLigthningCount = 41,          // 번개탄 전이 횟수 증가
        //UpgradeLigthningDamage = 42,         // 번개탄 데미지 증가
        //UpgradeLightningRange = 43,          // 번개탄 전이 범위 증가
        //UpgradeLightningChance = 44,         // 번개탄 확률 증가

        //// 관통탄
        //ActivatePierceBullet = 50,           // 관통탄 활성화
        //UpgradePierceCount = 51,             // 관통횟수 증가
        //UpgradePierceDamage = 52,            // 관통탄 데미지 증가

        //// 얼음탄
        //ActivateIceBullet = 60,              // 얼음탄 활성화
        //UpgradeIceSlowValue = 61,            // 슬로우 % 증가
        //UpgradeFreezeChance = 62,            // 빙결 걸릴 확률 증가
        //UpgradeIceSlowTime = 63,             // 슬로우 시간 증가
        //UpgradeFreezeTime = 64,              // 빙결 시간 증가

        //// 유도탄        
        //ActivateHomingBullet = 70,           // 유도탄 활성화
        //UpgradeHomingBulletRange = 71,       // 유도탄 범위 증가
        //                                     // 유도탄 탐색 범위 증가
        //// TODO : 화염탄
        //ActivateFireBullet = 80,             // 화염탄 활성화
        //UpgradeFireBulletDamage = 81,        // 화염탄 데미지 증가
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
        HomingBullet = 8,
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
        BulletBounceCountUp,        // 불릿 튕기는 횟수 증가
        UpdateGameTime,                 // 게임 시간 업데이트
        
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

