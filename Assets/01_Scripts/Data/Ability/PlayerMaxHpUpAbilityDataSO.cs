using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "MaxHpUpAbility", menuName = "ScriptableObjects/Ability/Player/MaxHpUp")]
public class PlayerMaxHpUpAbilityDataSO : PlayerAbilityDataSO
{
    [Header("레벨별 최대 체력 증가량")]
    // 구조체 필요 없이 그냥 float 리스트면 충분합니다!
    public List<float> maxHpIncreases = new List<float>();
    public override void ApplyLevelUp(int level, PlayerStat targetStat)
    {
        if (level <= 0 || level > maxHpIncreases.Count) return;

        // 타겟(플레이어)의 스피드 스탯에 바로 더해줍니다!
        float amount = maxHpIncreases[level - 1];
        Managers.Game._player.UpdateMaxHp(amount);
    }
}

