using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[CreateAssetMenu(fileName = "EffectData", menuName = "Data/EffectData")]
public class EffectDataSO : ScriptableObject
{
    [Serializable]
    public class EffectInfo
    {
        public EffectType type;          
        public GameObject prefab;
    }

    // 인스펙터에서 리스트 형태로 쭈욱 추가할 수 있습니다.
    public List<EffectInfo> effectList = new List<EffectInfo>();
}