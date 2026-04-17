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
}
public abstract class BulletStatDataSO : ScriptableObject
{

    public Define.BulletType type;
    [Header("Base Stat")]
    public BaseBulletStatData stats;

    [Header("Common Settings")]
    public GameObject originalPrefab;

    public bool isReload;   // 얻었을 때 장전할건지 안할건지 체크

    public abstract BaseBulletStat CreateRuntimeStat();
}




