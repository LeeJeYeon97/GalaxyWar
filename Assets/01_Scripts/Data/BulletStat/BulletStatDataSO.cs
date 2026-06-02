using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;


[Serializable]
public struct BaseBulletStatData
{
    public Sprite CardIcon;
    public Sprite hudIcon;
    public float chance;
    public float damage;
    public float speed;
    public float bounceCount;
}

[System.Serializable]
public class BulletConfigWrapper
{
    public List<BulletBalanceData> bulletList;
}

[System.Serializable]
public struct BulletBalanceData
{
    public string type;
    public bool isReload;

    // [공통 스탯 (BaseBulletStatData)]
    public float chance;
    public float damage;
    public float speed;
    public float bounceCount;

    // [Normal 특화]
    public int pierceCount;
    public float pierceDamageDecreaseValue;

    // [Lightning 특화]
    public float lightningDamageValue;
    public int lightningCount;
    public float lightningRange;

    // [Explosion 특화]
    public float explosionRange;
    public float explosionDamageValue;

    // [Ice 특화]
    public float slowValue;
    public float slowTime;
    public float freezeTime;

    public float fireDamageValue;
    public float fireRemainTime;        // 화상 지속 시간
    public float fireZoneDestroyTime;
    public float fireZoneSize;

    public float homingShotDelay;

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






