using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


[CreateAssetMenu(fileName = "PlayerDamageUp", menuName = "ScriptableObjects/Ability/Player/PlayerDamageUp")]
public class PlayerDamageUpAbilityDataSO : PlayerAbilityDataSO
{
    [Header("레벨별 데미지 증가량")]
    // 구조체 필요 없이 그냥 float 리스트면 충분합니다!
    public List<float> maxHpIncreases = new List<float>();

    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel < 0 || nextLevel > maxHpIncreases.Count)
        {
            return null;
        }
        // UI 띄우기용 데이터 전달
        return new object[] { maxHpIncreases[nextLevel] };
    }
    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level < 0 || level > maxHpIncreases.Count) return;

        // 모든 탄환의 공격력 증가
        foreach (var stat in Managers.Stat.bulletStatDict)
        {
            // 데미지 비율로 증가
            stat.Value.damage.AddMultiplier(maxHpIncreases[level]);
        }
        Managers.Event.PostEvent(Define.ActionEvent.BulletDamageUp);
    }
}