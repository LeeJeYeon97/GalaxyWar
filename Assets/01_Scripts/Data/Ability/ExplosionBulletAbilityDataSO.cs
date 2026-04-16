using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "ExplosionBulletAbilityData", menuName = "ScriptableObjects/Ability/ExplosionBullet")]
public class ExplosionBulletAbilityDataSO : BulletAbilityDataSO
{
    [Header("폭발탄 스탯 증가량")]
    public List<ExplosionBulletStatData> stats = new List<ExplosionBulletStatData>();

    public override object[] GetUpgradeValues()
    {
        int currentLevel = Managers.Ability.GetCurrentLevel(type);

        // 현재 레벨이 0이면 (최초 획득 상황), 증가량이 없으므로 null이나 기본값을 반환
        // UI 쪽에서는 이 경우 "범위 공격을 하는 폭발탄을 획득합니다." 같은 고정 텍스트를 띄움
        
        if (currentLevel <= 0 || currentLevel >= stats.Count)
        {
            return null; // 최대 레벨 도달
        }

        // UI 번역 텍스트의 {0}, {1}에 들어갈 수치! (데미지가 먼저인지 범위가 먼저인지 번역 테이블과 순서를 꼭 맞추세요)
        return new object[] { stats[currentLevel].explosionRange, stats[currentLevel].explosionDamageValue };
    }

    public override void ApplyLevelUp(int level, BaseBulletStat targetStat)
    {
        base.ApplyLevelUp(level, targetStat);

        if (level < 0 || level > stats.Count)  return;

        ExplosionBulletStatData data = stats[level];

        if (targetStat is ExplosionBulletStat stat)
        {
            stat.explosionRange.AddValue(data.explosionRange);
            stat.explosionDamage.AddValue(data.explosionDamageValue);

            Debug.Log($"폭발탄 Lv.{level + 1} 강화 완료! 추가 범위: +{data.explosionRange}, 추가 데미지: +{data.explosionDamageValue}");
        }
    }
}