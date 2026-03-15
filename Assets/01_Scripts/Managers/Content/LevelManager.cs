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
        // 1단계: 초반 폭풍 성장 구간 (1 ~ 5레벨)
        // 5 -> 10 -> 15 -> 20 -> 25 (경험치 1짜리 메테오 기준)
        // 의도: 무기가 1개뿐인 초반에 지루할 틈 없이 연속으로 스킬을 고르게 만듭니다.
        if (CurrentLevel <= 5)
        {
            return CurrentLevel * 5;
        }

        //  2단계: 중반 텐션 유지 구간 (6 ~ 20레벨)
        // 40 -> 55 -> 70 ... -> 250
        // 의도: 이쯤 되면 스폰량이 늘어나고, 메테오 경험치도 3으로 오릅니다. 
        // 유저가 한창 몹을 쓸어 담는 재미를 느낄 때라 요구량을 살짝 가파르게 올립니다.
        else if (CurrentLevel <= 20)
        {
            int midStep = CurrentLevel - 5;
            return 25 + (midStep * 15);
        }

        //  3단계: 후반 하드코어 구간 (21레벨 이상)
        // 300 -> 360 -> 430 -> 510 ... (2차 함수로 폭발적 증가)
        // 의도: 최종 스킬 진화(궁극기)를 앞두고 요구량이 기하급수적으로 늘어납니다.
        // 메테오가 경험치를 10~50씩 주지만, 잡기 힘들어지므로 레벨업이 아주 간절해집니다.
        else
        {
            int lateStep = CurrentLevel - 20;
            return 250 + (lateStep * 50) + (lateStep * lateStep * 5);
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
