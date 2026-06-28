using System.Collections.Generic;
using System.Globalization;
using UnityEngine;


[System.Serializable]
public struct DropItemRate
{
    public Define.ItemType itemType; // 드롭할 아이템 종류
    [Range(0f, 1f)]
    public float dropRate;          // 0.0(0%) ~ 1.0(100%) 확률
}
[System.Serializable]
public class MeteorConfigWrapper
{
    public List<MeteorBalanceData> meteorList;
}

[System.Serializable]
public struct MeteorBalanceData
{
    //  핵심: Define.MeteorType 이 아니라 일단 string으로 받습니다!
    public string Type;
    public bool IsExclude;
    public float MaxHp;
    public float MaxSpeed;
    public float MinSpeed;
    public float Damage;
    public float Score;
    public float Exp;

    public string minPhase;
    public string maxPhase;
    public float weight;

    public bool targetChase;

    //  특수 기믹 밸런스 수치 추가!
    public float magmaTick;
    public float auraRadius;

    public float poisonTick;
    public float poisonDamage;
    public float poisonRadius;

    public float explosionRadius;
    public float explosionDelay;
    public float explosionTargetRadius;

    public List<DropItemRateString> dropTable;
}
//  드랍 아이템용 구조체도 string으로 받을 수 있게 하나 만들어 줍니다.
[System.Serializable]
public struct DropItemRateString
{
    public string itemType;
    public float dropRate;
}

[CreateAssetMenu(fileName = "MeteorStatData", menuName = "ScriptableObjects/MeteorStatData")]
public class MeteorStatDataSO : ScriptableObject
{
    public Define.MeteorType Type;

    public GameObject originalPrefabs;
    
    public bool isExclude; 

    public float MaxHp;
    public float MaxSpeed;
    public float MinSpeed;
    public float Damage;

    public float Score;
    public float Exp;

    [Header("Phase Settings")]
    public Define.PhaseType minPhase; // 등장하기 시작하는 페이즈 (예: 2)
    public Define.PhaseType maxPhase; // 마지막으로 등장하는 페이즈 (예: 3, 즉 4부터는 안 나옴. 0이면 무한히 나옴)

    [Header("Spawn Chance")]
    public float weight; // 스폰 가중치 (이 값이 높을수록 자주 뽑힘)
    
    public bool targetChase;

    [Header("Drop Item Settings")]
    public List<DropItemRate> dropTable = new List<DropItemRate>();

    [Header("Magma Meteor Setting")]
    public float magmaTick;
    public GameObject magmaPuddle;

    [Header("Sludge Meteor Setting")]
    public GameObject sludgePuddle;

    [Header("Fracture Meteor Setting")]
    public GameObject fragmentMeteor;

    [Header("Aura Meteor Setting")]
    // 오라버프
    public float auraRadius;

    [Header("Poison Meteor Setting")]
    public float poisonTick;
    public float poisonDamage;
    public float poisonRadius;

    [Header("Explosion Meteor Setting")]
    public float explosionRadius;
    public float explosionDelay;
    public float explosionTargetRadius;
}
