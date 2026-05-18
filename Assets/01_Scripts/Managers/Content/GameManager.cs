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

    public PlayerController _player;
    public Spawner spawner;

    public int reviveCount { get; set; } // 광고 봤을 때 사용가능한 살아나기 횟수
    public int killCount { get; set; }
    public float gamePlayTime;
    public int currentSessionGold;
    
    public void Init()
    {
        gamePlayTime = 0f;
        currentSessionGold = 0;
        killCount = 0;
        ChangeGameState(GameState.Pause);

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
        if (Managers.Game.currentGameState == GameState.Playing)
        {
            gamePlayTime += Time.deltaTime;
            Managers.Event.PostEvent(ActionEvent.UpdateGameTime, gamePlayTime);
            Managers.Stage.UpdateWaveTimeline(gamePlayTime);
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
            default:
                break;
        }
    }

    public void Clear()
    {
        ClearAllBullets();
        ClearAllMeteors();
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
