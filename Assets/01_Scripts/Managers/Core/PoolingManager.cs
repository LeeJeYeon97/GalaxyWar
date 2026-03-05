using System.Collections.Generic;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Pool;

public class PoolingManager
{

    // 1. 원본 프리팹을 보관할 캐시 (SO에서 읽어온 것)
    private Dictionary<Define.Pool, GameObject> _prefabDic = new Dictionary<Define.Pool, GameObject>();

    // 2. 실제 풀링 시스템
    private Dictionary<string, IObjectPool<GameObject>> _pools = new Dictionary<string, IObjectPool<GameObject>>();
    // 풀링된 오브젝트들이 하이어라키에서 지저분하지 않게 정리할 루트
    private Transform _root;

    public void Init()
    {
        if (Managers.Data.poolingData == null)
        {
            Debug.Log("PoolingManager Init Error Data Null");
        }

        if (_root == null)
        {
            _root = new GameObject("@Pool_Root").transform;
            Object.DontDestroyOnLoad(_root);
        }

        // 오리지널 프리팹 담는 딕셔너리
        foreach (var pool in Managers.Data.poolingData.poolList)
        {
            if (!_prefabDic.ContainsKey(pool.type))
            {
                _prefabDic.Add(pool.type, pool.original);
            }
        }
    }

    // 내부적으로 풀을 생성하는 함수
    private void CreatePool(Define.Pool type)
    {

        string key = type.ToString();
        GameObject originalPrefab = _prefabDic[type]; // 미리 저장해둔 원본 사용

        IObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () => {
                GameObject go = Object.Instantiate(originalPrefab);
                go.name = key;
                go.transform.SetParent(_root);
                return go;
            },
            actionOnGet: (go) => go.SetActive(true),
            actionOnRelease: (go) => go.SetActive(false),
            actionOnDestroy: (go) => Object.Destroy(go),
            maxSize: 100
        );
        _pools.Add(key, pool);
    }

    // 핵심 함수: 꺼내기 (컴포넌트 타입으로 바로 가져옴)
    public T Get<T>(Define.Pool type, Transform parent = null) where T : UnityEngine.Object
    {
        string key = type.ToString();
        // 1. 해당 경로(이름)의 풀이 있는지 확인하고 없으면 만듦
        if (!_pools.ContainsKey(key))
        {
            // 만약 프리팹 등록도 안 되어 있다면 에러
            if (!_prefabDic.ContainsKey(type))
            {
                Debug.LogError($"[PoolingManager] {type} 타입의 프리팹이 SO에 등록되지 않았습니다!");
                return null;
            }
            CreatePool(type);
        }

        GameObject go = _pools[key].Get();
        if (parent != null) go.transform.SetParent(parent);

        if (typeof(T) == typeof(GameObject)) return go as T;
        T component = go.GetComponent<T>();
        // 3. T가 컴포넌트라면 GetComponent로 찾아보기
        
        // 4. 컴포넌트가 없다면? 코드로 직접 붙여주기!
        if (component == null)
        {
            // AddComponent<T>() 대신 AddComponent(typeof(T))를 사용하여 제네릭 제약 조건 우회
            component = go.AddComponent(typeof(T)) as T;
        }

        return component;

    }

    // 핵심 함수: 반납하기
    public void Release(GameObject go)
    {
        if (go == null || !go.activeSelf) return;
        string key = go.name;

        if (_pools.ContainsKey(key))
            _pools[key].Release(go);
        else
            Object.Destroy(go);
    }

    public void Clear()
    {
        foreach (var pool in _pools.Values) pool.Clear();
        foreach (Transform child in _root) Object.Destroy(child.gameObject);
        _pools.Clear();
        _prefabDic.Clear();
    }
}
