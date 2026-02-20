using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class Stat // (또는 BaseStat)
{
    [SerializeField]
    protected float _baseValue;
    [SerializeField]
    protected float _additionalValue;
    [SerializeField]
    protected float _multiplier = 1f;
    // 강제 0 스위치
    private bool _isForcedZero = false;
    public float TotalValue
    {
        get
        {
            if (_isForcedZero) return 0; // 스위치가 켜져 있으면 계산 생략하고 0 반환
            return (_baseValue + _additionalValue) * _multiplier;
        }
    }

    public virtual void Init(float baseValue)
    {
        _baseValue = baseValue;
        _additionalValue = 0;
        _multiplier = 1f;
    }
    public void SetForceZero(bool active) => _isForcedZero = active;
    public void AddValue(float amount) => _additionalValue += amount;
    public void AddMultiplier(float amount) => _multiplier += amount;
    public void SubValue(float amount) => _additionalValue -= amount;
    public void SubMultiplier(float amount) => _multiplier -= amount;

}

