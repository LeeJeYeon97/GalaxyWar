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
        // 1. 유저님이 제안하신 완벽한 기본 공식 (1, 2, 3, 4... 씩 늘어나는 수학 공식)
        // n(n+1)/2 공식을 쓰면 반복문 없이 깔끔하게 계산됩니다!
        int n = CurrentLevel - 1;
        float baseRequired = 5 + (n * (n + 1)) / 2;

        // 2. 초반 (1~10레벨): 운석이 경험치 1을 주므로 공식을 그대로 씁니다.
        if (CurrentLevel <= 10)
        {
            return baseRequired;
        }
        // 3. 중반 (11~20레벨): 운석이 경험치를 3~10씩 주므로 요구량도 가파르게 올립니다.
        else if (CurrentLevel <= 20)
        {
            float midStep = CurrentLevel - 10;
            return baseRequired + (midStep * 20); // 레벨당 20씩 추가 페널티
        }
        // 4. 후반 (21레벨 이상): 운석이 경험치를 50씩 주므로 요구량을 확 늘립니다!
        else
        {
            float lateStep = CurrentLevel - 20;
            return baseRequired + 200 + (lateStep * 80); // 후반용 폭발적 증가
        }
    }
    public void AddExp(float exp)
    {
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
