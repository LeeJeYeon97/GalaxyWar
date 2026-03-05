using System;
using UnityEngine;
using static Define;

public class LevelManager
{

    public int CurrentLevel { get; private set; } = 1;
    public float CurrentExp { get; private set; } = 0;

    public int Score { get; private set; } = 0;

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
        return Managers.Data.GameData.baseMaxExp * Mathf.Pow(Managers.Data.GameData.maxExpMultiplier, CurrentLevel - 1);
    }
    public void AddExp(float blockLevel)
    {
        float exp = Managers.Data.GameData.baseExpGain + ((blockLevel - 1) * Managers.Data.GameData.expGainIncreasePerLevel);
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
        CurrentExp -= MaxExp; // 남은 경험치 이월
        CurrentLevel++;

        // 1. 게임 일시정지 (시간 배율 0)
        Managers.Game.ChangeGameState(Define.GameState.Pause);

        // 2. 레벨업 이벤트 알림 (UI 매니저 등에서 듣고 팝업을 띄움)
        Managers.Event.PostEvent<int>(ActionEvent.LevelUp, CurrentLevel);

        Managers.UI.ShowPopupUI<UI_LevelUpPopup>();

        // 3. UI 갱신 (경험치가 바로 다음 레벨로 넘어갈 수도 있으므로 재귀 체크)
        Managers.Event.PostEvent<(float,float)>(ActionEvent.ExpChanged, (CurrentExp, MaxExp));

        if (CurrentExp >= MaxExp) LevelUp();
    }
}
