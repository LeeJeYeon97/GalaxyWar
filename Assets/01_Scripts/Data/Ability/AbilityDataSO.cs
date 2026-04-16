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

    public Sprite icon;
    public int maxLevel = 5;

    // 필요한 선행능력
    public Define.AbilityType _requiredAbility = AbilityType.Unknown;

    // 핵심! 자식 클래스들이 각자 자기 상황에 맞게 오버라이드할 가상 함수
    public abstract object[] GetUpgradeValues();
}
