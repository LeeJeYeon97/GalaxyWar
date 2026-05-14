using System.Collections.Generic;
using UnityEngine;


public class UIManager
{
    int _order = 10;

    Stack<UI_Popup> _popupStack = new Stack<UI_Popup>();
    UI_Scene _sceneUI = null;
    public UI_Transition Transition;

    public GameObject Root
    {
        get
        {
            GameObject root = GameObject.Find("@UI_Root");
            if (root == null)
                root = new GameObject { name = "@UI_Root" };
            return root;
        }
    }

    public void Init()
    {
        // 씬 전환 애니메이션용 프리팹
        GameObject go  = Managers.Resource.Instantiate("UI/SubItem/UI_Transition");
        Object.DontDestroyOnLoad(go);
        Transition = go.GetComponent<UI_Transition>();
    }
    public void Clear()
    {
        CloseAllPopupUI();
        if(_sceneUI != null)
        {
            Managers.Resource.Destroy(_sceneUI.gameObject);
        }
    }
    public void SetCanvas(GameObject go, bool sort = true)
    {
        Canvas canvas = Util.GetOrAddComponent<Canvas>(go);

        //  1. UI 오브젝트에 붙어있는 스크립트(UI_Base)를 찾아서, 대표님이 설정한 모드를 알아냅니다.
        UI_Base uiBase = go.GetComponent<UI_Base>();
        RenderMode targetMode = RenderMode.ScreenSpaceCamera; // 기본값

        if (uiBase != null)
        {
            targetMode = uiBase.canvasRenderMode; // 프리팹에서 설정한 값 가져오기
        }

        // 2. 유니티가 멋대로 바꿨든 말든, 우리가 원하는 모드로 강제 세팅해버립니다!
        canvas.renderMode = targetMode;

        //  3. Camera 모드라면 메인 카메라를 찰칵!
        if (targetMode == RenderMode.ScreenSpaceCamera)
        {
            canvas.worldCamera = Camera.main;
            if (canvas.worldCamera == null)
                Debug.LogWarning($"[UI 매니저] {go.name}에 할당할 MainCamera를 찾을 수 없습니다.");
        }

        canvas.overrideSorting = true;

        if (sort)
        {
            canvas.sortingOrder = _order;
            _order++;
        }
        else
        {
            canvas.sortingOrder = 0;
        }
    }
    public T MakeSubItem<T>(Transform parent = null, string name = null) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/SubItem/{name}");
        if (parent != null)
            go.transform.SetParent(parent);

        return Util.GetOrAddComponent<T>(go);
    }
    public T ShowSceneUI<T>(string name = null) where T : UI_Scene
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/Scene/{name}");
        T sceneUI = Util.GetOrAddComponent<T>(go);

        _sceneUI = sceneUI;

        go.transform.SetParent(Root.transform);

        return sceneUI;
    }
    public T ShowPopupUI<T>(string name = null) where T : UI_Popup
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/Popup/{name}");
        T popup = Util.GetOrAddComponent<T>(go);
        _popupStack.Push(popup);

        go.transform.SetParent(Root.transform);

        return popup;
    }
    public void ClosePopupUI(UI_Popup popup)
    {
        if (_popupStack.Count == 0)
            return;

        if (_popupStack.Peek() != popup)
        {
            Debug.Log("Close Popup Failed!");
            return;
        }

        ClosePopupUI();
    }
    public void ClosePopupUI()
    {
        if (_popupStack.Count == 0)
            return;

        UI_Popup popup = _popupStack.Pop();

        Managers.Resource.Destroy(popup.gameObject);
        popup = null;
        _order--;
    }
    public void CloseAllPopupUI()
    {
        while (_popupStack.Count > 0)
            ClosePopupUI();
    }

}
