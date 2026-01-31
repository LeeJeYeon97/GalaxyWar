using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityData", menuName = "ScriptableObjects/AbilityData")]
public class AbilityDataSO : ScriptableObject
{
    [Header("Ability Info")]
    public Define.AbilityType type; // 능력 종류
    public string abilityname;      // 스킬이름

    [TextArea]
    public string description;      // 스킬설명
    public Sprite icon;

    [Header("스킬 레벨 설계")]
    public int maxLevel = 5;
    public int curLevel = 0;

    [Header("선행 조건 설정")]
    // 이 값이 Unknown이면 선행 조건이 없는 것임
    public Define.AbilityType _requiredAbility = Define.AbilityType.Unknown;

    public List<float> values = new List<float>();

    // 현재 레벨에 맞는 수치를 가져오는 헬퍼 함수
    public float GetValue(int level)
    {
        if (level <= 0 || level > values.Count) return 0;

        return values[level - 1];
    }
}
