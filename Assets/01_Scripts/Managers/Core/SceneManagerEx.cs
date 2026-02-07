using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerEx
{
    public BaseScene CurrentScene
    {
        get
        {
            // 변수가 비어있을 때만 찾습니다.
            if (_currentScene == null)
                _currentScene = GameObject.FindAnyObjectByType<BaseScene>();
            return _currentScene;
        }
    }
    private BaseScene _currentScene; // 캐싱용 변수

    public void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    public void Clear()
    {
        //SceneManager.sceneLoaded -= OnSceneLoaded;
        _currentScene = null;
        CurrentScene.Clear();
    }
    public void LoadScene(Define.Scene type)
    {
        Managers.Instance.Clear();
        SceneManager.LoadScene(GetSceneName(type));
    }

    string GetSceneName(Define.Scene type)
    {
        string name = System.Enum.GetName(typeof(Define.Scene), type);
        return name;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. 현재 씬에 BaseScene을 상속받은 객체가 있는지 확인
        BaseScene sceneObj = GameObject.FindAnyObjectByType<BaseScene>();

        // 2. 없다면 생성
        if (sceneObj == null)
        {
            // @Scene이라는 이름으로 빈 오브젝트 생성
            GameObject go = new GameObject { name = "@Scene" };

            // 씬 타입에 맞는 컴포넌트를 동적으로 붙여줍니다. (예: GameScene, LobbyScene 등)
            // 씬 이름이 "GameScene"이라면 "GameScene"이라는 클래스를 찾아 붙입니다.
            string sceneName = scene.name;
            System.Type sceneType = System.Type.GetType(sceneName);

            if (sceneType != null)
                go.AddComponent(sceneType);
            else
                Debug.LogWarning($"{sceneName} 타입의 스크립트를 찾을 수 없습니다.");
        }
    }
}
