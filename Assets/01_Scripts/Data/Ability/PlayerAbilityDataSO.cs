using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public abstract class PlayerAbilityDataSO : AbilityDataSO
{
    [Header("공통 스탯 증가량")]
    public List<PlayerStatData> baseStats = new List<PlayerStatData>();
    // ★ 다형성의 꽃! 자식들이 무조건 구현해야 하는 '스탯 적용' 가상 함수
    // 밖에서는 이 함수 하나만 부르면, 각 총알이 알아서 자기 스탯을 올립니다!
    public abstract void ApplyLevelUp(int level, PlayerStat targetStat);
}

