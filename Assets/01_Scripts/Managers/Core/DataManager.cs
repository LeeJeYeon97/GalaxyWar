using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;


//public interface ILoader<Key, Value>
//{
//    Dictionary<Key, Value> MakeDict();
//}

public class DataManager 
{
    // 모든 데이터 담고 있는 딕셔너리
    public Dictionary<Define.BulletType, BulletStatDataSO> BulletDataDict { get; private set; }
    public Dictionary<Define.AbilityType, AbilityDataSO> AbilityDataDict { get; private set; }

    public GameDataSO GameData { get; private set; }
    public PlayerStatDataSO playerStatData { get; private set; }
    public PoolingDataSO poolingData { get; private set; }
    public Dictionary<Define.ItemType,ItemDataSO> ItemDataList { get; private set; }

    public Dictionary<Define.MeteorType, MeteorStatDataSO> MeteorStatDataDict {  get; private set; }
    public SoundDataSO SoundData { get; private set; }

    public void Init()
    {
        // [사용 예시]
        // 1. Bullets는 SO의 'name'을 키로 사용
        BulletDataDict = LoadDataToDict<Define.BulletType, BulletStatDataSO>("Bullets", data => data.type);

        // 2. Abilities는 만약 내부에 'abilityID' 같은 별도 필드가 있다면 그것을 키로 사용
        // AbilityDataDict = LoadDataToDict<string, AbilityDataSO>("Abilities", data => data.abilityID);
        AbilityDataDict = LoadDataToDict<Define.AbilityType, AbilityDataSO>("Abilities", data => data.type);

        GameData = Managers.Resource.Load<GameDataSO>(Path.GameData);
        if(GameData == null)
        {
            Debug.LogError("GameData Null");
        }
        playerStatData = Managers.Resource.Load<PlayerStatDataSO>(Path.PlayerStatData);
        if (playerStatData == null)
        {
            Debug.LogError("playerStatData Null");
        }

        poolingData = Managers.Resource.Load<PoolingDataSO>(Path.PoolingData);
        if (poolingData == null)
        {
            Debug.LogError("poolingData Null");
        }
        SoundData = Managers.Resource.Load<SoundDataSO>(Path.SoundData);
        if (SoundData == null)
        {
            Debug.LogError("SoundData Null");
        }

        ItemDataList = LoadDataToDict<Define.ItemType, ItemDataSO>("Items",data => data.type);
        MeteorStatDataDict = LoadDataToDict<Define.MeteorType, MeteorStatDataSO>("Meteors", data => data.Type);
    }

    /// <summary>
    /// 데이터를 로드하여 지정한 키 생성 규칙에 따라 딕셔너리를 생성합니다.
    /// </summary>
    /// <typeparam name="TKey">딕셔너리의 키 타입 (string, int, Enum 등)</typeparam>
    /// <typeparam name="TValue">데이터 타입 (ScriptableObject 상속체)</typeparam>
    private Dictionary<TKey, TValue> LoadDataToDict<TKey, TValue>(string folderName, Func<TValue, TKey> keySelector) where TValue : ScriptableObject
    {
        Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
        TValue[] datas = Managers.Resource.LoadAll<TValue>($"Datas/{folderName}");

        if (datas == null || datas.Length == 0)
        {
            Debug.LogWarning($"DataManager: Datas/{folderName} 경로에 데이터가 없습니다.");
            return dict;
        }

        foreach (var data in datas)
        {
            // keySelector 델리게이트를 실행하여 데이터에서 키를 뽑아냄
            TKey key = keySelector(data);

            if (key == null) continue;

            if (!dict.ContainsKey(key))
                dict.Add(key, data);
            else
                Debug.LogWarning($"DataManager: 중복된 키 감지! Type: {typeof(TValue)}, Key: {key}");
        }

        return dict;
    }

    private List<T> LoadDataToList<T>(string folderName) where T : ScriptableObject
    {
        List<T> list = new List<T>();
        T[] datas = Managers.Resource.LoadAll<T>($"Datas/{folderName}");

        if (datas == null || datas.Length == 0)
        {
            Debug.LogWarning($"DataManager: Datas/{folderName} 경로에 데이터가 없습니다.");
            return null;
        }

        list = datas.ToList<T>();
        return list;
    }
    


    // T로 들어온 스크립터블 오브젝트로 만든 모든 데이터들중에 이름으로 찾아주는 함수
    //public T GetData<T>(string name) where T : ScriptableObject
    //{
    //    // 내가 찾고 싶은 데이터 타입
    //    System.Type type = typeof(T);
    //
    //    if(_allDataDict.TryGetValue(type,out object dictObj))
    //    {
    //        var dict = dictObj as Dictionary<string, T>;
    //
    //        if(dict != null && dict.TryGetValue(name, out T data))
    //        {
    //            return data;
    //        }
    //    }
    //    return null;
    //}

    //// T로 들어온 스크립터블 오브젝트로 만든 모든 데이터를 리스트로 반환하는 함수
    //public List<T> GetAllData<T> () where T : ScriptableObject
    //{
    //    System.Type type = typeof(T);
    //
    //    if (_allDataDict.TryGetValue(type, out object dictObj))
    //    {
    //        var dict = dictObj as Dictionary<string, T>;
    //
    //        if(dict != null)
    //        {
    //            return new List<T>(dict.Values);
    //        }
    //    }
    //    Debug.LogError($"DataManager: {type.Name} 타입의 데이터가 존재하지 않습니다.");
    //  
    //    return null;
    //}

    //Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    //{
    //    TextAsset textAsset = Managers.Resource.Load<TextAsset>($"Data/{path}");
    //    return JsonUtility.FromJson<Loader>(textAsset.text);
    //}
}


