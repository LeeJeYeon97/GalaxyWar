using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Localization;
using static Define;

[System.Serializable]
public class AbilityConfigWrapper
{
    public List<AbilityBalanceData> abilityList;
}

[System.Serializable]
public struct AbilityBalanceData
{
    public string type; // Define.AbilityType
    public int maxLevel;
    public string requiredAbility;

    public string bulletType;

    //  핵심 1: 원본 BaseBulletStatData를 쓰면 아이콘이 날아가므로, 숫자만 받을 전용 구조체를 리스트로 씁니다!
    public List<BaseBulletStatJsonData> baseStats;

    //  핵심 2: 폭발 스탯에는 Sprite가 없으므로 기존에 만드신 구조체를 그대로 리스트로 받아도 됩니다.
    public List<ExplosionBulletStatData> explosionBulletStats;
    public List<FireBulletStatData> fireBulletStats;
    public List<IceBulletStatData> iceBulletStats;
    public List<LightningBulletStatData> lightningBulletStats;
    public List<HomingBulletStatData> homingBulletStats;
    public List<PierceBulletStatData> pierceBulletStats;

    
    
    // [추가된 변수] 플레이어 바운스 패시브 레벨업 데이터
    public List<BulletBounceLevelData> bounceIncreases;
    public List<BurstModeStat> burstModeIncreases;
    public List<CriticalData> criticalAbilityData;
    public List<float> damageUpAbilityData;
    public List<float> playerHealData;
    public List<float> maxHpUpData;
    public List<float> reloadCountUpData;
    public List<float> reloadTimeDownData;
    public List<int> ShieldAbilityData;
    public List<float> shotTimeDownData;
    public List<float> speedUpData;
    public List<SplitShotLevelData> splitBulletData;
    public List<mineStatData> mineStats;
}

// 아이콘(Sprite)을 제외하고 서버에서 받아올 순수 수치들만 담은 구조체
[System.Serializable]
public struct BaseBulletStatJsonData
{
    public float chance;
    public float damage;
    public float speed;
    public float bounceCount;
}

public abstract class AbilityDataSO : ScriptableObject
{
    [Header("공통 정보")]
    public Define.AbilityType type;

    public Sprite icon;
    public int maxLevel = 5;

    // 필요한 선행능력
    public Define.AbilityType _requiredAbility = AbilityType.Unknown;

    // 핵심! 자식 클래스들이 각자 자기 상황에 맞게 오버라이드할 가상 함수
    public abstract object[] GetUpgradeValues();
}
