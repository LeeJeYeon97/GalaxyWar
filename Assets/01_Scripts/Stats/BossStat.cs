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

        MaxHp.Init(data.MaxHp);

        Speed.Init(data.Speed);

        Damage.Init(data.Damage);

        myPatterns = data.myPatterns;
    }
}

