using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public abstract class BulletAbilityDataSO : AbilityDataSO
{
    public Define.BulletType bulletType;
    [Header("공통 스탯 증가량")]
    public List<BaseBulletStatData> baseStats = new List<BaseBulletStatData>();
    // ★ 다형성의 꽃! 자식들이 무조건 구현해야 하는 '스탯 적용' 가상 함수
    // 밖에서는 이 함수 하나만 부르면, 각 총알이 알아서 자기 스탯을 올립니다!
    public abstract void ApplyLevelUp(int level, BaseBulletStat targetStat);
}

