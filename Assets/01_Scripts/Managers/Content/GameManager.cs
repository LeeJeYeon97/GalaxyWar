using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using static Define;

public class GameManager : MonoBehaviour
{
    public GameState currentGameState;
    public GameState previousGameState;

    public List<MeteorController> activeMeteors = new List<MeteorController>();
    public List<BulletController> activeBullets = new List<BulletController>();

    public int Score { get; private set; } = 0;

    public Action<float> OnUpdateScore;

    public PlayerController _player;
    public void Init()
    {
     
    }
    public void SetGame()
    {
        Managers.Map.Init();

        Score = 0;
        ChangeGameState(GameState.Playing);

        // 플레이어 세팅
        GameObject go = Managers.Resource.Instantiate("Prefabs/Object/Player");
        _player = go.GetComponent<PlayerController>();
        _player?.Init();

        GameObject spawner = Managers.Resource.Instantiate(Path.Spawner);
        spawner.GetComponent<Spawner>()?.Init();

        Managers.UI.ShowSceneUI<UI_GameScene>();
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
            Time.timeScale = 1.0f;
            // 이전 상태로 되돌리고 함수 종료 (아래의 currentGameState = state를 실행하지 않음)
            ChangeGameState(previousGameState);
            return;
        }

        // 새로운 상태를 적용하기 전에 현재 상태를 저장 (Pause로 갈 때만 필요하다면)
        if (state == GameState.Pause)
        {
            previousGameState = currentGameState;
            Time.timeScale = 0f;
        }

        // 실제 상태 변경
        currentGameState = state;

        switch (currentGameState)
        {
            case GameState.Playing:
                // 게임 시작 이벤트
                // 플레이어 소환
                break;
            case GameState.Pause:
                // 일시정지 시 추가 로직 (UI 띄우기 등)
                break;
            case GameState.GameOver:
                // 게임 오버 이벤트
                break;
            default:
                break;
        }
    }

    public void ClearAllBullets()
    {
        if (activeBullets.Count == 0) return;

        // 리스트를 뒤에서부터 순회하거나, foreach를 쓰되 반복문 안에서 
        // 리스트를 직접 수정하지 않도록 주의해야 합니다.
        // 뒤에서부터 순회 (count - 1부터 0까지)
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            // 리스트의 i번째 요소를 참조하여 처리
            Managers.Pool.Release(activeBullets[i].gameObject);
        }
    }

    public void AddScore(int score)
    {
        if(score <= 0)
        {
            return;
        }
        Score += score;
        OnUpdateScore?.Invoke(Score);
    }
    public BulletStat GetRandomBullet()
    {
        // 1. 활성화된(Unlocked) 스탯들만 따로 모을 리스트를 직접 만듭니다.
        List<BulletStat> activeStats = new List<BulletStat>();
        int totalWeight = 0;

        // 2. 전체 딕셔너리를 돌면서 체크합니다. (LINQ의 Where + Sum 역할)
        foreach (var stat in Managers.Stat.bulletStatDict.Values)
        {
            if (stat.isActivated)
            {
                activeStats.Add(stat);
                totalWeight += (int)stat.chance.TotalValue; // 합계도 동시에 구합니다.
            }
        }

        // 예외 처리: 활성화된 탄환이 없으면 기본탄 반환
        if (activeStats.Count == 0 || totalWeight == 0)
        {
            return Managers.Stat.GetBulletStat(BulletType.NormalBullet);
        }

        // 3. 당첨 번호 뽑기
        int pivot = UnityEngine.Random.Range(0, totalWeight);
        int currentWeight = 0;

        // 4. 어떤 구간에 당첨됐는지 순회하며 확인
        for (int i = 0; i < activeStats.Count; i++)
        {
            currentWeight += (int)activeStats[i].chance.TotalValue;

            if (pivot < currentWeight)
            {
                // 당첨! 해당 타입에 맞는 데이터를 가져옵니다.
                return activeStats[i];
            }
        }

        return Managers.Stat.GetBulletStat(BulletType.NormalBullet);
    }

    // 테스트용
    public void TestAbility()
    {
        // 1. 게임 일시정지 (시간 배율 0)
        Managers.Game.ChangeGameState(Define.GameState.Pause);

        Managers.UI.ShowPopupUI<UI_GameTestPopup>();
        
    }
    
    
}
