using System.Collections.Generic;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Pool;

public class PoolingManager
{

    // 프리팹 이름(Key)별로 실제 풀(Value)을 관리
    private Dictionary<string, IObjectPool<GameObject>> _pools = new Dictionary<string, IObjectPool<GameObject>>();
    // Enum -> String 변환 시 가비지 발생을 막기 위한 캐시
    private Dictionary<Define.Pool, string> _nameCache = new Dictionary<Define.Pool, string>();
    // 풀링된 오브젝트들이 하이어라키에서 지저분하지 않게 정리할 루트
    private Transform _root;

    public void Init()
    {
        if (_root == null)
        {
            _root = new GameObject("@Pool_Root").transform;
            Object.DontDestroyOnLoad(_root);
        }
    }

    // 핵심 함수: 꺼내기 (컴포넌트 타입으로 바로 가져옴)
    public T Get<T>(Define.Pool type, Transform parent = null) where T : UnityEngine.Object
    {
        string key = GetKey(type);
        // 1. 해당 경로(이름)의 풀이 있는지 확인하고 없으면 만듦
        if (!_pools.ContainsKey(key))
        {
            CreatePool<T>(key);
        }

        // 2. 풀에서 꺼냄 (GameObject를 꺼내서 T 컴포넌트를 반환)
        GameObject go = _pools[key].Get();
        if (parent != null) go.transform.SetParent(parent);
        // --- 수정된 부분 ---

        // 만약 T가 GameObject 그 자체라면, 컴포넌트를 찾지 않고 go를 바로 리턴합니다.
        if (typeof(T) == typeof(GameObject))
        {
            return go as T;
        }

        return go.GetComponent<T>();
    }

    // 핵심 함수: 반납하기
    public void Release(GameObject go)
    {
        // 1. 이미 비활성화된 객체라면, 풀에 이미 들어갔다는 뜻이므로 무시
        if (go == null || !go.activeSelf)
            return;

        // 이름 뒤에 (Clone)이 붙어있으므로 이를 떼고 이름으로 풀을 찾음
        string key = go.name;

        if (_pools.ContainsKey(key))
        {
            _pools[key].Release(go);
        }
        else
        {
            // 풀이 없는 객체라면 그냥 파괴
            Object.Destroy(go);
        }
    }

    // 내부적으로 풀을 생성하는 함수
    private void CreatePool<T>(string path)
    {
        IObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () => {
                GameObject go = Managers.Resource.Instantiate($"Prefabs/{path}");
                go.name = path; // (Clone) 관리를 위해 이름 고정
                go.transform.SetParent(_root);

                // T가 컴포넌트 타입인지 확인하고, 맞다면 컴포넌트를 붙여줌
                if (typeof(Component).IsAssignableFrom(typeof(T)))
                {
                    // T가 컴포넌트라면 GetComponent를 시도하고 없으면 AddComponent 함
                    if (go.GetComponent(typeof(T)) == null)
                        go.AddComponent(typeof(T));
                }
                return go;
            },
            actionOnGet: (go) => go.SetActive(true),
            actionOnRelease: (go) => go.SetActive(false),
            actionOnDestroy: (go) => Object.Destroy(go),
            maxSize: 100
        );

        _pools.Add(path, pool);
    }


    public void Clear()
    {
        foreach (var pool in _pools.Values)
        {
            // ObjectPool의 경우 Clear를 호출해주는 것이 안전함
            pool.Clear();
        }

        foreach (Transform child in _root)
            Object.Destroy(child.gameObject);

        _pools.Clear();
        _nameCache.Clear();
    }

    // Enum을 String으로 변환하고 캐싱하는 헬퍼 함수
    private string GetKey(Define.Pool type)
    {
        if (!_nameCache.ContainsKey(type))
        {
            _nameCache[type] = type.ToString();
        }
        return _nameCache[type];
    }
}
