using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "BossStatData", menuName = "ScriptableObjects/BossStatData")]
public class BossStatDataSO : ScriptableObject
{
    public Define.BossType Type;

    public GameObject originalPrefab;
    public GameObject bossBulletPrefab;

    public float MaxHp;
    public float Speed;
    public float Damage;

    public float Score;

    [Header("Drop Item Settings")]
    public List<DropItemRate> dropTable = new List<DropItemRate>();

    // 이 보스가 사용할 패턴들을 인스펙터에서 리스트로 넣어줍니다!
    [Header("사용 패턴")]
    public List<BossPatternSO> myPatterns;

}


