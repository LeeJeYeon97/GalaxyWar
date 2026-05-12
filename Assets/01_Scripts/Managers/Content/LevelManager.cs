using System;
using UnityEngine;
using static Define;

public class LevelManager
{

    public int CurrentLevel { get; private set; } = 1;
    public float CurrentExp { get; private set; } = 0;

    public int Score { get; private set; } = 0;

    public int PendingLevelUpCount = 0;

    public float MaxExp => GetMaxExp();
    public void Init()
    {
        CurrentLevel = 1;
        CurrentExp = 0;
        Score = 0;

        Managers.Event.PostEvent<(float, float)>(ActionEvent.ExpChanged, (CurrentExp, MaxExp));
        Managers.Event.PostEvent<int>(ActionEvent.LevelUp, CurrentLevel);
        Managers.Event.PostEvent<float>(ActionEvent.ScoreChanged, Score);
    }

    public float GetMaxExp()
    {
        // n은 레벨업을 위한 가중치 역할 (1레벨일 땐 n=0)
        int n = CurrentLevel - 1;

        // 1. 기본 요구량 (2차 함수의 가파름을 줄이고, 선형 증가 비중을 높였습니다)
        // 초반 레벨업이 훨씬 부드러워지며, 20레벨까지 기분 좋게 성장합니다.
        float baseRequired = 20 + (n * 20) + (n * n * 2.5f);

        // 2. 초반~중반 (1~20레벨): 도파민 분비 구간! (페널티 없음)
        // 15~20레벨까지는 수월하게 빌드업할 수 있도록 기본 공식만 적용합니다.
        if (CurrentLevel <= 20)
        {
            return baseRequired;
        }
        // 3. 중후반 (21~30레벨): 운석이 쏟아지는 구간 (페널티 시작)
        // 플레이어가 강해졌으므로, 21레벨부터는 레벨당 요구량이 눈에 띄게 증가합니다.
        else if (CurrentLevel <= 30)
        {
            float midStep = CurrentLevel - 20;
            return baseRequired + (midStep * 100);
        }
        // 4. 극후반 (31레벨 이상): 억제기 풀가동!
        // 오라, 분열 메테오 등 고가치(50~70) 메테오를 잡아야만 렙업이 가능하게 꽉 묶습니다.
        else
        {
            float lateStep = CurrentLevel - 30;
            return baseRequired + 1000 + (lateStep * 300);
        }
    }
    public void AddExp(float exp)
    {
        if(Managers.Data.GameData.expZero == true)
        {
            return;
        }
        CurrentExp += exp;

        // UI 업데이트를 위해 이벤트 호출
        Managers.Event.PostEvent<(float, float)>(ActionEvent.ExpChanged, (CurrentExp, MaxExp));

        // 레벨업 판단
        if (CurrentExp >= MaxExp)
        {
            LevelUp();
        }
    }

    public void AddScore(int score)
    {
        if (score <= 0)
        {
            return;
        }
        Score += score;
        Managers.Event.PostEvent<float>(ActionEvent.ScoreChanged, Score);
    }


    private void LevelUp()
    {
        // 1. 재귀(Recursion) 제거! while문으로 남은 경험치를 모두 정산합니다.
        while (CurrentExp >= MaxExp)
        {
            CurrentExp -= MaxExp;
            CurrentLevel++;
            PendingLevelUpCount++; // 팝업을 띄워야 할 횟수 1 증가

            // 레벨업 이벤트 알림 (단순 UI 숫자 갱신용)
            Managers.Event.PostEvent<int>(ActionEvent.LevelUp, CurrentLevel);
        }

        // 2. 팝업 띄우기 및 게임 정지 처리
        if (PendingLevelUpCount > 0)
        {
            // 이미 Pause 상태가 아닐 때만 정지시킴 (이전 상태 덮어쓰기 방지)
            if (Managers.Game.currentGameState != Define.GameState.Pause)
            {
                Managers.Game.ChangeGameState(Define.GameState.Pause);
            }

            // 팝업 띄우기 (이미 떠있다면 알아서 무시되거나 최상단으로 올라옴)
            Managers.UI.ShowPopupUI<UI_LevelUpPopup>();
        }
        // UI 갱신
        Managers.Event.PostEvent<(float, float)>(ActionEvent.ExpChanged, (CurrentExp, MaxExp));
    }
}
