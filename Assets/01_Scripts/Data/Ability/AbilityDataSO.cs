using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Localization;
using static Define;

public abstract class AbilityDataSO : ScriptableObject
{
    [Header("공통 정보")]
    public Define.AbilityType type;
    public LocalizedString localizedName;
    public List<LocalizedString> localizationDesc = new List<LocalizedString>();

    public Sprite icon;
    public int maxLevel = 5;

    // 필요한 선행능력
    public Define.AbilityType _requiredAbility = AbilityType.Unknown;

    public LocalizedString GetLevelDescription(int level)
    {
        {
            if (level <= 0 || level > localizationDesc.Count) return null;
            return localizationDesc[level - 1];
        }
    }

}
