using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using static Define;

public abstract class AbilityDataSO : ScriptableObject
{
    [Header("공통 정보")]
    public Define.AbilityType type;
    public LocalizedString localizedName;
    public Sprite icon;
    public int maxLevel = 5;

    // 필요한 선행능력
    public Define.AbilityType _requiredAbility = AbilityType.Unknown;

}
