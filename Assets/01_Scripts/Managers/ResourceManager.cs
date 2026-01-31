using UnityEngine;

public class ResourceManager
{
    // 프리팹 로드 함수
    public T Load<T>(string path) where T : Object
    {
        return Resources.Load<T>(path);
    }

    public T[] LoadAll<T>(string path) where T : Object
    {
        return Resources.LoadAll<T>(path);
    }
    // 생성까지 한 번에 해주는 함수
    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject prefab = Load<GameObject>($"{path}");
        if (prefab == null) return null;

        return Object.Instantiate(prefab, parent);
    }
    public void Destroy(GameObject go)
    {
        if(go == null) return;

        Object.Destroy(go);
    }
}
