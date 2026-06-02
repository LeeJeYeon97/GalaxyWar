using NUnit.Framework.Internal;
using System;
using TMPro;
using UnityEngine;

public class Managers : MonoBehaviour
{

    static bool _isQuitting = false;

    private static Managers _instance;
    public static Managers Instance
    {
        get
        {
            if (_instance == null)
            {
                Init();
            }
            return _instance;
        }
    }
    // ========================================================== //
    // 각 관리자들 (필요한 매니저들을 여기에 추가)
    private InitManager _init;
    private InputManager _input;
    private PoolingManager _pool;                    
    private GameManager _game;
    private ResourceManager _resource;
    private LevelManager _level;
    private UIManager _ui;
    private AbilityManager _ability;
    private DataManager _data;
    private EventManager _event;
    private StatManager _stat;
    private SceneManagerEx _scene;
    private SoundManager _sound;
    private MapManager _map;
    private CoroutineHelper _coroutine;
    private LoginManager _login;
    private SettingManager _setting;
    private EffectManager _effect;
    private StageManager _stage;

    private PlayerDataManager _playerData;
    private PlayerEconomyManager _playerEconomy;
    private AdsManager _ad;
    private VirtualStoreManager _virtualStore;
    private IAPStoreManager _iapStore;
    private RemoteConfigManager _remoteConfig;

    // ========================================================== //
    // 프로퍼티를 통해 외부에서 접근하도록 설정
    public static InitManager Initialize => Instance._init;
    public static InputManager Input => Instance._input;
    public static PoolingManager Pool => Instance._pool;
    public static GameManager Game => Instance._game;
    public static ResourceManager Resource => Instance._resource;
    public static LevelManager Level => Instance._level;
    public static UIManager UI => Instance._ui;
    public static AbilityManager Ability => Instance._ability;
    public static DataManager Data => Instance._data;
    public static StatManager Stat => Instance._stat;
    public static SceneManagerEx Scene => Instance._scene;
    public static SoundManager Sound => Instance._sound;
    public static MapManager Map => Instance._map;
    public static EventManager Event => Instance._event;
    public static AdsManager AD => Instance._ad;

    public static LoginManager Login => Instance._login;
    public static PlayerDataManager PlayerData => Instance._playerData;
    public static PlayerEconomyManager PlayerEconomy => Instance._playerEconomy;
    public static VirtualStoreManager VirtualStore => Instance._virtualStore;
    public static IAPStoreManager IAPStore => Instance._iapStore;
    public static SettingManager Setting => Instance._setting;
    public static EffectManager Effect => Instance._effect;

    public static StageManager Stage => Instance._stage;
    public static RemoteConfigManager RemoteConfig => Instance._remoteConfig;
    // ========================================================== //

    // 추가: 외부에서 Managers.Coroutine.StartCoroutine() 으로 접근할 수 있게 열어줍니다!
    public static CoroutineHelper Coroutine => Instance._coroutine;

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }
    static void Init()
    {
        if (_isQuitting) return;

        if (_instance == null)
        {
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            _instance = go.GetComponent<Managers>();

            // 컴포넌트로 붙일 필요없는 매니저 초기화
            _instance._init = new InitManager();
            _instance._scene = new SceneManagerEx();
            _instance._resource = new ResourceManager();
            _instance._ui = new UIManager();
            _instance._pool = new PoolingManager();
            _instance._level = new LevelManager();
            _instance._ability = new AbilityManager();
            _instance._data = new DataManager();
            _instance._stat = new StatManager();
            _instance._sound = new SoundManager();
            _instance._map = new MapManager();
            _instance._event = new EventManager();
            _instance._ad = new AdsManager();
            _instance._login = new LoginManager();
            _instance._playerData = new PlayerDataManager();
            _instance._playerEconomy = new PlayerEconomyManager();
            _instance._virtualStore = new VirtualStoreManager();
            _instance._iapStore = new IAPStoreManager();
            _instance._setting = new SettingManager();
            _instance._effect = new EffectManager();
            _instance._stage = new StageManager();
            _instance._remoteConfig = new RemoteConfigManager();

            // 컴포넌트 형태의 매니저들은 여기서 초기화하거나 자식으로 붙임
            _instance._game = Util.GetOrAddComponent<GameManager>(go);
            _instance._input = Util.GetOrAddComponent<InputManager>(go);
            _instance._coroutine = Util.GetOrAddComponent<CoroutineHelper>(go);

            // 매니저들 초기화 함수
            // Core
            // InitManager초기화는 처음 Scene에서 진행하기

            // 로그 안보이게
            Debug.unityLogger.logEnabled = Debug.isDebugBuild;

            PlayerData.Init();
            PlayerEconomy.Init();
            VirtualStore.Init();
            IAPStore.Init();

            AD.Init();
            Pool.Init();
            Data.Init();
            _ = RemoteConfig.InitAsync();
            Stage.Init();
            Input.Init();
            Sound.Init();
            Effect.Init();
            Scene.Init();
            UI.Init();
            Setting.Init();

            Application.targetFrameRate = 60; // 60프레임 고정 (부드러운 화면)

        }
    }
    private void Update()
    {
        // 다른 매니저들 업데이트 돌리기
    }
    private void LateUpdate()
    {
        Effect.OnLateUpdate();
    }
    public void Clear()
    {
        _scene.Clear();
        _sound.Clear();
        _ui.Clear();

        if (_coroutine != null)
        {
            _coroutine.StopAllCoroutines();
        }
    }
}
