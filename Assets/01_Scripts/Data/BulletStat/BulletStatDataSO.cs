using System;
using UnityEngine;
using UnityEngine.Localization;


[Serializable]
public struct BaseBulletStatData
{
    public float chance;
    public float damage;
    public float speed;
    public float bounceCount;
    public bool isReload;
}
public abstract class BulletStatDataSO : ScriptableObject
{

    public Define.BulletType type;
    [Header("Base Stat")]
    public BaseBulletStatData stats;

    [Header("Common Settings")]
    public GameObject originalPrefab;

    public abstract BaseBulletStat CreateRuntimeStat();
}




