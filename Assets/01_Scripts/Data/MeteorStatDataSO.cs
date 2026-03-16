using System.Globalization;
using UnityEngine;


[CreateAssetMenu(fileName = "MeteorStatData", menuName = "ScriptableObjects/MeteorStatData")]
public class MeteorStatDataSO : ScriptableObject
{
    public Define.MeteorType Type;
    public Sprite Sprite;
    public string Name;

    public bool isExclude; 

    public float MaxHp;
    public float MaxSpeed;
    public float MinSpeed;
    public float Damage;

    public float Score;
    public float Exp;
}
