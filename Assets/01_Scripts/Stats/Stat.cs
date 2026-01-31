using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class Stat // (¶Ç´Â BaseStat)
{
    [SerializeField]
    protected float _baseValue;
    [SerializeField]
    protected float _additionalValue;
    [SerializeField]
    protected float _multiplier = 1f;
    public float TotalValue => (_baseValue + _additionalValue) * _multiplier;

    public virtual void Init(float baseValue)
    {
        _baseValue = baseValue;
        _additionalValue = 0;
        _multiplier = 1f;
    }

    public void AddValue(float amount) => _additionalValue += amount;
    public void AddMultiplier(float amount) => _multiplier += amount;
}

