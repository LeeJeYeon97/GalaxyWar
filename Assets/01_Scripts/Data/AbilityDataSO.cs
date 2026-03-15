using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "AbilityData", menuName = "ScriptableObjects/AbilityData")]
public class AbilityDataSO : ScriptableObject
{
    [Header("Ability Info")]
    public Define.AbilityType type;

    // ★ 기존 string을 LocalizedString으로 교체!
    public LocalizedString localizedName;       // 스킬 이름 (번역 키 연결용)
    public LocalizedString localizedDescription; // 스킬 설명 (스마트 스트링용)

    public Sprite icon;

    [Header("스킬 레벨 설계")]
    public int maxLevel = 5;
    public int curLevel = 0;

    [Header("선행 조건 설정")]
    public Define.AbilityType _requiredAbility = Define.AbilityType.Unknown;

    public List<float> values = new List<float>();

    public float GetValue(int level)
    {
        if (level <= 0 || level > values.Count) return 0;
        return values[level - 1];
    }
}
