using UnityEngine;

public class ResourceManager
{
    public T Load<T>(string path) where T : Object
    {
        if (typeof(T) == typeof(GameObject))
        {
            string name = path;
            int index = name.LastIndexOf('/');
            if (index >= 0)
                name = name.Substring(index + 1);

            GameObject go = Managers.Pool.GetOriginal(name);
            if (go != null)
                return go as T;
        }
        return Resources.Load<T>(path);
    }
    public T[] LoadAll<T>(string path) where T : Object
    {
        return Resources.LoadAll<T>(path);
    }

    // [추가] 프리팹 원본(GameObject)을 직접 받는 Instantiate
    public GameObject Instantiate(GameObject original, Transform parent = null)
    {
        if (original == null)
        {
            Debug.Log($"Failed to instantiate : Original is null");
            return null;
        }

        // 풀링이 필요한 아이템인지 체크 (Poolable 컴포넌트 유무)
        if (original.GetComponent<Poolable>() != null)
            return Managers.Pool.Get(original, parent).gameObject;

        // 일반 생성
        GameObject go = Object.Instantiate(original, parent);
        go.name = original.name;
        return go;
    }

    // [리팩토링] 기존 경로 기반 함수가 위 함수를 호출하도록 변경
    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject original = Load<GameObject>($"Prefabs/{path}");
        if (original == null)
        {
            Debug.Log($"Failed to load prefab : {path}");
            return null;
        }

        return Instantiate(original, parent);
    }

    public void Destroy(GameObject go)
    {
        if (go == null) return;

        //Poolable poolable = go.GetComponent<Poolable>();
        //if (poolable != null)
        //{
        //    Managers.Pool.Release(go);
        //    return;
        //}

        //Object.Destroy(go);

        // GetComponent 연산 생략 가능
        if (go.TryGetComponent<Poolable>(out var p) && p.IsPooled)
        {
            Managers.Pool.Release(go);
        }
        else
        {
            Object.Destroy(go);
        }
    }
}

