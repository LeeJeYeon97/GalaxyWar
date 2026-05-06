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

    public Define.PhaseType spawnPhase;

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
}
