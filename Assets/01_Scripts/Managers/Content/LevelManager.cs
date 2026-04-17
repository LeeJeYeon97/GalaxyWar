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
        // 새로운 메테오 경험치(최소 10 ~ 최대 70)에 맞춰 스케일업된 공식!
        int n = CurrentLevel - 1;

        // 1. 기본 요구량 (기존 5 -> 20으로 상향)
        // n(n+1)/2 공식에 15를 곱해서 초반 레벨업 템포를 기가 막히게 조절합니다.
        float baseRequired = 20 + ((n * (n + 1)) / 2f) * 15;

        // 2. 초반 (1~10레벨): Phase 1~2 구간. 공식을 그대로 씁니다.
        if (CurrentLevel <= 10)
        {
            return baseRequired;
        }
        // 3. 중반 (11~20레벨): Phase 3~4 구간. 운석이 0.5초마다 쏟아지므로 페널티를 확 늘립니다.
        else if (CurrentLevel <= 20)
        {
            float midStep = CurrentLevel - 10;
            return baseRequired + (midStep * 150); // 기존 20 -> 150으로 페널티 강화
        }
        // 4. 후반 (21레벨 이상): Phase 5 구간. 오라, 분열 메테오가 50~70씩 주므로 억제기를 켭니다!
        else
        {
            float lateStep = CurrentLevel - 20;
            return baseRequired + 1500 + (lateStep * 400); // 후반용 폭발적 증가
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
