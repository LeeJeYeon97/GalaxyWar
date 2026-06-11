using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static Define;

public class GameManager : MonoBehaviour
{
    public GameState currentGameState;
    public GameState previousGameState;

    public List<MeteorController> activeMeteors = new List<MeteorController>();
    public List<BulletController> activeBullets = new List<BulletController>();
    // 상단 변수 선언부에 아이템 리스트 추가
    public List<ItemController> activeItems = new List<ItemController>();

    public PlayerController _player;
    public Spawner spawner;

    public int reviveCount { get; set; } // 광고 봤을 때 사용가능한 살아나기 횟수
    public int killCount { get; set; }
    public float gamePlayTime;
    public int currentSessionGold;

    private bool _isTargetTimeReached = false; // 10분 도달 체크용 플래그
    private bool _isWarningTimeReached = false; //  [추가] 5초 전 경고 체크용 플래그

    private Camera mainCam;

    public bool isBossSpawn;

    public void Init()
    {
        gamePlayTime = 0f;
        currentSessionGold = 0;
        _isWarningTimeReached = false;
        _isTargetTimeReached = false;
        isBossSpawn = false;
        killCount = 0;
        ChangeGameState(GameState.Pause);
        mainCam = Camera.main;

        mainCam.DOOrthoSize(Managers.Data.GameData.gamePlayeSize, 0.3f)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnUpdate(() =>
            {

                Managers.Map.UpdateMap();
            });

        // UI 생성
        UI_GameScene sceneUI = Managers.UI.ShowSceneUI<UI_GameScene>();

        // 맵 생성
        Managers.Map.Init();

        Managers.Stage.CalculateStageBaseDifficulty();
        // 레벨
        Managers.Level.Init();

        // 어빌리티
        Managers.Ability.Init();

        // 스탯 매니저
        Managers.Stat.Init();

        
        // 플레이어 세팅
        _player = Managers.Resource.Instantiate("Object/Player")?.GetComponent<PlayerController>();
        if (_player == null)
            return;
        _player.Init();

        spawner = Managers.Resource.Instantiate("Object/Spawner")?.GetComponent<Spawner>();
        if (spawner == null)
            return;

        reviveCount = Managers.Data.GameData.reviveCount;
        
    }
    private void Update()
    {
        if (Managers.Game.currentGameState != GameState.Playing) return;
        
        gamePlayTime += Time.deltaTime;
        Managers.Event.PostEvent(ActionEvent.UpdateGameTime, gamePlayTime);
        Managers.Stage.UpdateWaveTimeline(gamePlayTime);

        //  [추가] 10분 도달 5초 전에 위험 팝업 실시간 체크
        float warningTargetTime = Managers.Data.GameData.gameclearTime - 5f;
        if (!_isWarningTimeReached && gamePlayTime >= warningTargetTime)
        {
            _isWarningTimeReached = true;
            OnWarningTimeReached();
        }

        // 10분에 도달했는지 실시간 체크
        if (!_isTargetTimeReached && gamePlayTime >= Managers.Data.GameData.gameclearTime)
        {
            _isTargetTimeReached = true;
            OnTargetTimeReached();
        }
    }
    /// <summary>
    /// 10분(목표 시간)에 도달했을 때 실행되는 함수
    /// </summary>
    private void OnTargetTimeReached()
    {
        // StageManager에게 "여기 보스 스테이지야?" 라고 물어봅니다.
        if (Managers.Stage.IsBossStage)
        {
            mainCam.DOOrthoSize(Managers.Data.GameData.bossStageSize, 0.3f)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnUpdate(() =>
            {
                Managers.Map.UpdateMap();
            });

            // 1. 일반 메테오 스폰 속도를 무한대로 늘리거나 스포너를 멈춤
            spawner.StopSpawn(); // 혹은 스폰 딜레이를 9999로 변경

            isBossSpawn = true;

            // 맵에 깔린 모든 잔몹과 경험치를 깔끔하게 청소!
            ClearAllMeteors();
            AbsorbAllItemsAndLevelUp();

            Managers.Game._player.GetComponentInChildren<AttackRangeIndicator>().HideCircle();
            // 2. 보스 스폰 로직 호출 (StageManager가 들고 있는 프리팹과 HP 사용)
            BossStat stat = Managers.Stat.GetRandomBossStat();
            if(stat == null)
            {
                Debug.LogError("bossStat is null");
                return;
            }
            spawner.BossSpawn(stat);
        }
        else
        {
            ChangeGameState(GameState.GameClear);
        }
    }

    /// <summary>
    ///  [추가] 목표 시간 5초 전에 실행되는 경고 함수
    /// </summary>
    private void OnWarningTimeReached()
    {
        // 보스 스테이지일 때만 위험 경고 팝업을 띄웁니다!
        if (Managers.Stage.IsBossStage)
        {
            
            // 프로젝트에 만들어두신 위험 팝업 클래스 이름을 넣으시면 됩니다! (예: UI_WarningPopup)
            Managers.UI.ShowPopupUI<UI_BossWarningPopup>(); 

            // 꿀팁: 사이렌 소리 같은 Sfx를 이때 같이 재생해주면 몰입감이 200% 증가합니다.
            Managers.Sound.Play(Define.SoundID.Sfx_BossWarning);
        }
    }
    public void AddActiveObject<T>(T item)
    {
        // 1. 아이템이 Bullet 타입인지 확인
        if (item is BulletController bullet)
        {
            if (activeBullets.Contains(bullet) == false)
            {
                activeBullets.Add(bullet);
            }
        }
        // 2. 아이템이 BrickController 타입인지 확인
        else if (item is MeteorController brick)
        {
            if (activeMeteors.Contains(brick) == false)
            {
                activeMeteors.Add(brick);
            }
        }
        //  [추가] 아이템 리스트 관리
        else if (item is ItemController dropItem)
        {
            if (activeItems.Contains(dropItem) == false)
                activeItems.Add(dropItem);
        }
    }

    public void RemoveActiveObject<T>(T item)
    {
        // 1. 아이템이 Bullet 타입인지 확인
        if (item is BulletController bullet)
        {
            if (activeBullets.Contains(bullet))
                activeBullets.Remove(bullet);
        }
        // 2. 아이템이 BrickController 타입인지 확인
        else if (item is MeteorController brick)
        {
            if (activeMeteors.Contains(brick))
                activeMeteors.Remove(brick);
        }
        //  [추가] 아이템 리스트 관리
        else if (item is ItemController dropItem)
        {
            if (activeItems.Contains(dropItem))
                activeItems.Remove(dropItem);
        }
    }
    public void AbsorbAllItemsAndLevelUp()
    {
        if (activeItems.Count == 0) return;

        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            ItemController item = activeItems[i];

            // 2. 시각적으로 빨려 들어가는 연출 실행
            item.AbsorbToPlayer(_player.transform);
        }
    }
    public void ChangeGameState(GameState state)
    {
        // Resume은 '상태'라기보다 '동작'에 가깝습니다.
        if (state == GameState.Resume)
        {
            // 이전 상태로 되돌리고 함수 종료 (아래의 currentGameState = state를 실행하지 않음)
            ChangeGameState(previousGameState);
            return;
        }

        // 새로운 상태를 적용하기 전에 현재 상태를 저장 (Pause로 갈 때만 필요하다면)
        if (state == GameState.Pause)
        {
            previousGameState = currentGameState;
            
        }

        // 실제 상태 변경
        currentGameState = state;

        switch (currentGameState)
        {
            case GameState.Ready:
                break;
            case GameState.Playing:
                // 게임 시작 이벤트
                spawner.StartSpawn();
                _player?.SetState(PlayerState.Playing);
                break;
            case GameState.Pause:
                break;
            case GameState.GameOver:
                spawner.StopSpawn();
                _player?.SetState(PlayerState.Die);
                Managers.UI.ShowPopupUI<UI_GameOverPopup>();
                // 게임 오버 이벤트
                break;
            case GameState.GameClear:
                spawner.StopSpawn();
                _player?.SetState(PlayerState.Idle);
                Managers.UI.ShowPopupUI<UI_GameClearPopup>();
                break;
            default:
                break;
        }
    }

    public void Clear()
    {
        ClearAllBullets();
        ClearAllMeteors();
        ClearAllItems();
        Managers.Pool.Clear();
        Managers.Stat.Clear();
    }
    public void RevivePlayer()
    {
        ClearAllBullets();
        ClearAllMeteors();

        // 2. 플레이어 체력 회복 (PlayerController에 회복 함수가 있다고 가정)
        _player.Revive();
        if(reviveCount >= 1)
        {
            reviveCount--;
        }
        // 보스가 스폰 되지 않았을 때만 스폰을 풀기
        if(isBossSpawn == false)
        {
            spawner.UnlockSpawn();
        }
        // 3. 게임 상태를 다시 Playing으로 변경
        ChangeGameState(GameState.Playing);
    }
    private void ClearAllBullets()
    {
        if (activeBullets.Count == 0) return;

        // 리스트를 뒤에서부터 순회하거나, foreach를 쓰되 반복문 안에서 
        // 리스트를 직접 수정하지 않도록 주의해야 합니다.
        // 뒤에서부터 순회 (count - 1부터 0까지)
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            // 리스트의 i번째 요소를 참조하여 처리
            Managers.Resource.Destroy(activeBullets[i].gameObject);
        }
    }

    public void ClearAllMeteors()
    {
        if (activeMeteors.Count == 0) return;

        for (int i = activeMeteors.Count - 1; i >= 0; i--)
        {
            Managers.Resource.Destroy(activeMeteors[i].gameObject);
        }
        activeMeteors.Clear(); // 리스트 비우기
    }
    public void ClearAllItems()
    {
        if (activeItems.Count == 0) return;

        // 뒤에서부터 순회하며 안전하게 파괴(또는 풀링 반납)
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            Managers.Resource.Destroy(activeItems[i].gameObject);
        }
        activeItems.Clear(); // 리스트 비우기
    }
    // 테스트용
    public void TestAbility()
    {
        // 1. 게임 일시정지 (시간 배율 0)
        Managers.Game.ChangeGameState(Define.GameState.Pause);

        Managers.UI.ShowPopupUI<UI_GameTestPopup>();
    }
    public void AddKillCount()
    {
        killCount++;
        Managers.Event.PostEvent(Define.ActionEvent.MeteorDie);
    }
}
