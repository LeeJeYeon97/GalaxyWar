using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedUpAbility", menuName = "ScriptableObjects/Ability/Player/SpeedUp")]
public class PlayerSpeedUpAbilityDataSO : PlayerAbilityDataSO
{

    [Header("레벨별 이동 속도 증가량")]
    // 구조체 필요 없이 그냥 float 리스트면 충분합니다!
    public List<float> speedIncreases = new List<float>();

    public override object[] GetUpgradeValues()
    {
        int nextLevel = Managers.Ability.GetCurrentLevel(type);

        if (nextLevel < 0 || nextLevel > speedIncreases.Count)
        {
            return null;
        }
        // 폭발탄은 수치가 2개니까 2개만 배열로 묶어서 줍니다.
        return new object[] { speedIncreases[nextLevel]};
    }
    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level < 0 || level > speedIncreases.Count) return;

        // 타겟(플레이어)의 스피드 스탯에 바로 더해줍니다!
        float amount = speedIncreases[level];
        targetStat.speed.AddValue(amount);

    }
}

