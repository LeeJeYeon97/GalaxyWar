using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class BossStat
{
    public Define.BossType Type;

    public GameObject originalPrefab;
    public GameObject bossBulletPrefab;

    public Stat MaxHp = new Stat();
    public Stat Speed = new Stat();
    public Stat Damage = new Stat();

    public float Score;

    [Header("Drop Item Settings")]
    public List<DropItemRate> dropTable = new List<DropItemRate>();
    // 이 보스가 사용할 패턴들을 인스펙터에서 리스트로 넣어줍니다!

    [Header("사용 패턴")]
    public List<BossPatternSO> myPatterns;

    public void Init(BossStatDataSO data)
    {
        originalPrefab = data.originalPrefab;
        bossBulletPrefab = data.bossBulletPrefab;
        Type = data.Type;
        myPatterns = data.myPatterns;

        // =========================================================
        // 1. 스테이지 밸런스 배율 계산
        // =========================================================
        
        // 만약 UGS RemoteConfig로 받아오는 StageData에 증가율(증폭값) 변수가 있다면 그걸 쓰시면 됩니다.
        float hpIncreaseRate = Managers.Data.StageData.bossHpIncrease;
        float damageIncreaseRate = Managers.Data.StageData.bossDamageIncrease;
        float hpMultiplier = 1f + (hpIncreaseRate / 100);
        float damageMultiplier = 1f + (damageIncreaseRate / 100);


        // =========================================================
        // 2. 배율을 적용하여 최종 스탯 초기화
        // =========================================================

        // 원본 SO 데이터에 배율을 곱한 뒤 정수로 변환(권장)하여 세팅합니다.
        MaxHp.Init(Mathf.RoundToInt(data.MaxHp * hpMultiplier));

        Damage.Init(Mathf.RoundToInt(data.Damage * damageMultiplier));

        Speed.Init(data.Speed);

        // 점수(Score)도 스테이지가 오를수록 더 많이 주도록 배율을 적용할 수 있습니다.
        Score = data.Score * hpMultiplier;
    }
}

