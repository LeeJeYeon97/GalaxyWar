using System.Collections.Generic;
using UnityEngine;
using static Define;

public class EffectManager
{
    // Dictionary로 빠른 검색을 지원합니다.
    private Dictionary<EffectType, GameObject> _effectDict = new Dictionary<EffectType, GameObject>();

    public void Init()
    {
        // 2. Dictionary에 예쁘게 매핑해 둡니다.
        foreach (var effect in Managers.Data.EffectData.effectList)
        {
            _effectDict.Add(effect.type, effect.prefab);
        }
    }

    // 이펙트 재생 핵심 함수
    public void Play(EffectType type, Vector3 position)
    {
        if (_effectDict.TryGetValue(type, out GameObject prefab))
        {
            // Managers.Resource의 풀링 시스템을 이용해 이펙트를 가져옵니다!
            // (이름을 기반으로 프리팹을 Instantiate 하는 기존 기능 활용)
            GameObject effectGo = Managers.Resource.Instantiate(prefab);
            effectGo.transform.position = position;
        }
        else
        {
            Debug.LogWarning($"이펙트를 찾을 수 없습니다: {type}");
        }
    }
}
