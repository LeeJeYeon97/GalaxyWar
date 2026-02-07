using TMPro;
using UnityEngine;

public class Managers : MonoBehaviour
{

    static bool _isQuitting = false;

    private static Managers _instance;
    public static Managers Instance { get { Init(); return _instance; } }

    // ========================================================== //
    // 각 관리자들 (필요한 매니저들을 여기에 추가)
    private InputManager _input;
    private PoolingManager _pool;                    
    private GameManager _game;
    private ResourceManager _resource;
    private LevelManager _level;
    private UIManager _ui;
    private AbilityManager _ability;
    private DataManager _data;
    [SerializeReference] private StatManager _stat;
    private SceneManagerEx _scene;
    private SoundManager _sound;
    private MapManager _map;
    // ========================================================== //
    // 프로퍼티를 통해 외부에서 접근하도록 설정
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
    // ========================================================== //
    
    
    void Awake()
    {
        Init();
    }

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

            // 컴포넌트 형태의 매니저들은 여기서 초기화하거나 자식으로 붙임
            _instance._game = Util.GetOrAddComponent<GameManager>(go);
            _instance._input = Util.GetOrAddComponent<InputManager>(go);

            // 매니저들 초기화 함수
            Data.Init();
            UI.Init();
            Input.Init();
            Sound.Init();
            //Pool.Init();

            Level.Init();
            Scene.Init();

            Stat.Init();
            Ability.Init();
        }
    }
    public void Clear()
    {
        _scene.Clear();
        _sound.Clear();
        _ui.Clear();
        _input.Clear();
    }
    
}
