using System.Globalization;
using UnityEngine;


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

    [Header("Magma Meteor Setting")]
    public float magmaTick;
    public GameObject magmaPuddle;

    [Header("Sludge Meteor Setting")]
    public GameObject sludgePuddle;

    [Header("Aura Meteor Setting")]
    // 오라버프
    public float auraRadius;
}
