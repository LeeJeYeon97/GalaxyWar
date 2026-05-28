using Unity.Services.CloudCode.GeneratedBindings.Project;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using static Define;

public class StageManager
{

    public StageBalanceDataSO balanceData;
    public int currentStageLevel = 1;
    public int clearStageLevel = 0;

    public float CurrentSpawnDelay { get; private set; }

    //  숫자 인덱스 대신 Enum으로 현재 페이즈를 외부로 당당하게 노출! (UI 등에서 읽어갈 수 있음)
    public Define.PhaseType CurrentPhase { get; private set; }

    private float _stageBaseSpawnDelay;
    private float _stageHpBonus;
    private float _stageHpMultiplier;

    private float _currentWaveSpawnDelayRate = 1f;
    private float _currentWaveHpRate = 1f;
    private float _currentWaveSpeedRate = 1f;

    // 처음 시작할 때 페이즈가 강제 갱신되도록 돕는 스위치
    private bool _isPhaseInit = false;

    // Managers.cs가 게임 시작 시 딱 한 번 불러줄 초기화 함수
    public bool IsBossStage = false;
    public void Init()
    {
        if (balanceData == null)
        {
            balanceData = Managers.Data.StageData;
        }
        if (balanceData == null) return;

        Managers.PlayerData.PlayerDataUpdated += SetPlayerClearStage;

        _isPhaseInit = false; // 씬을 재시작하면 다시 초기화되도록 세팅
        IsBossStage = false;
        // 플레이어 데이터 보고 최대 클리어한 스테이지 체크
        //SetPlayerClearStage(Managers.PlayerData.PlayerDataLocal);
    }
    public void SetPlayerClearStage(PlayerData data)
    {
        if(data.MaxClearStage > 0)
        {
            clearStageLevel = data.MaxClearStage;
            currentStageLevel = data.MaxClearStage + 1;
        }
        else
        {
            clearStageLevel = 0;
            currentStageLevel = 1;
        }
    }
    /// <summary>
    /// [스테이지 진입 시 1회 호출] 
    /// 현재 스테이지(1, 2, 3...)에 따른 '기본 난이도 뼈대'를 수학적으로 계산합니다.
    /// </summary>
    public void CalculateStageBaseDifficulty()
    {
        // 1스테이지일 때 곱하기가 0이 되도록(아무런 보너스가 없도록) 인덱스를 1 깎아줍니다.
        // 예: 1스테이지 = 0, 15스테이지 = 14
        int levelIndex = currentStageLevel - 1;
        IsBossStage = (currentStageLevel % balanceData.bossStageInterval) == 0 ? true : false;

        // [스폰 속도 공식] (선형 감소)
        // 공식: 기본 2초 - (0.02초 * 스테이지 단계)
        _stageBaseSpawnDelay = balanceData.baseSpawnDelay - (balanceData.spawnDelayDecrease * levelIndex);

        // [스폰 속도 방어선] 
        // 스테이지가 700까지 가더라도, 렉 방지를 위해 최소 스폰 간격(예: 0.3초) 밑으로는 절대 안 내려가게 막습니다.
        _stageBaseSpawnDelay = Mathf.Max(balanceData.minSpawnDelay, _stageBaseSpawnDelay);

        //  [체력 증가 공식] (선형 + 복리 하이브리드)
        // 1. 선형 보너스: 매 스테이지마다 정직하게 +10씩 더해줍니다.
        _stageHpBonus = balanceData.hpGrowthPerStage * levelIndex;

        // 2. 복리 보너스: 매 스테이지마다 1.02배(2%)씩 곱연산으로 뻥튀기합니다. (Mathf.Pow는 제곱 계산)
        _stageHpMultiplier = Mathf.Pow(balanceData.hpMultiplierPerStage, levelIndex);

        // 계산된 기본 스폰 간격을 현재 스폰 간격으로 확정 짓습니다.
        CurrentSpawnDelay = _stageBaseSpawnDelay;

        Debug.Log($"[{currentStageLevel} 스테이지 세팅 완료] 기본 스폰간격: {_stageBaseSpawnDelay}초 , 보스스테이지 : {IsBossStage}");
    }
    /// <summary>
    /// [매 프레임 호출] 
    /// 10분의 플레이 타임 동안, 시간대별로 쏟아지는 물량/체력(Wave)을 실시간으로 갱신합니다.
    /// </summary>
    /// <param name="playTime">현재까지 생존한 게임 시간 (초)</param>
    public void UpdateWaveTimeline(float playTime)
    {
        // 시간표(waves) 데이터가 없으면 계산할 필요 없이 바로 리턴!
        if (balanceData.waves == null || balanceData.waves.Count == 0) return;

        WaveMultiplier targetWave = null;

        // 1. 현재 시간에 맞는 웨이브 데이터 찾기
        for (int i = 0; i < balanceData.waves.Count; i++)
        {
            if (playTime >= balanceData.waves[i].waveStartTime)
                targetWave = balanceData.waves[i]; // 조건을 만족하는 마지막 웨이브로 덮어씌워짐
            else
                break;
        }

        // 2. 새로운 페이즈 데이터가 있고, (처음이거나 페이즈가 달라졌다면)
        if (targetWave != null)
        {
            if (!_isPhaseInit || CurrentPhase != targetWave.phaseType)
            {
                //  페이즈 갱신!
                CurrentPhase = targetWave.phaseType;
                _isPhaseInit = true;

                _currentWaveSpawnDelayRate = targetWave.waveSpawnDelayRate;
                _currentWaveHpRate = targetWave.waveHpRate;
                _currentWaveSpeedRate = targetWave.speedRate;

                // 스폰 딜레이 최종 갱신
                CurrentSpawnDelay = Mathf.Max(balanceData.minSpawnDelay, _stageBaseSpawnDelay * _currentWaveSpawnDelayRate);

                Debug.Log($"[페이즈 변경!] 현재 페이즈: {CurrentPhase} | 게임 타임: {playTime:F1}초");

                // (선택) 만약 페이즈별로 BGM을 바꾸거나 보스 알림을 띄우고 싶다면 
                // 아래처럼 스위치문을 달아서 활용하실 수 있습니다!
                /*
                switch (CurrentPhase)
                {
                    case Define.PhaseType.Phase3:
                        Managers.UI.ShowWarning("중간 보스 등장!");
                        break;
                }
                */
            }
        }
    }

    /// <summary>
    /// [메테오 생성 시 호출]
    /// 메테오가 화면에 스폰될 때, 이 함수를 불러서 '최종적으로 뻥튀기된 체력'을 받아갑니다.
    /// </summary>
    /// <param name="meteorBaseHp">소환될 메테오 프리팹 고유의 기본 체력 (예: 일반 100, 탱커 300)</param>
    /// <returns>스테이지와 시간대 배율이 모두 적용된 최종 체력 수치</returns>
    public float GetCalculatedMeteorHp(float meteorBaseHp)
    {
        // [최종 HP 계산 공식]
        // 1. (meteorBaseHp + _stageHpBonus) : 본래 체력에 스테이지 기본 깡스탯을 더함
        // 2. * _stageHpMultiplier : 스테이지 복리 배율 곱하기
        // 3. * _currentWaveHpRate : 현재 시간대(Wave)의 특수 배율 곱하기
        float finalHp = (meteorBaseHp + _stageHpBonus) * _stageHpMultiplier * _currentWaveHpRate;

        return finalHp;
    }
    public float GetCalculatedMeteorSpeed(float meteorBaseSpeed)
    {

        float finalSpeed = (meteorBaseSpeed * _currentWaveSpeedRate);

        return finalSpeed;
    }
    public void Clear()
    {

        Managers.PlayerData.PlayerDataUpdated -= SetPlayerClearStage;
    }
}
