using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class PoisonMeteorBehavior : IMeteorBehavior
{
    public void OnDie(MeteorController meteor)
    {
        // 핵심: 메테오가 파괴될 때(체력이 0이 되거나 터질 때) 독 장판을 생성합니다!

        // 1. 풀링 매니저에서 독 장판 프리팹을 가져옵니다. (경로는 프로젝트에 맞게 수정)

        
        GameObject puddleGo = Managers.Effect.Play(Define.EffectType.MeteorPoison, Vector3.zero);

        if (puddleGo != null)
        {
            // 2. 죽은 메테오의 위치에 장판을 깝니다.
            puddleGo.transform.position = meteor.transform.position;

            // 3. 장판 스크립트를 찾아 데미지와 범위를 세팅해 줍니다.
            if (puddleGo.TryGetComponent(out PoisonZoneController poisonZone))
            {
                // 예시: 틱 데미지는 메테오 본체 데미지의 30%, 반경은 2.5f, 지속시간은 5초
                poisonZone.Init(meteor.Stat.poisonDamage.TotalValue, meteor.Stat.poisonRadius.TotalValue, meteor.Stat.poisonTick.TotalValue);
            }
        }
    }

    public void OnInit(MeteorController meteor)
    {
    }

    public void OnRelease(MeteorController meteor)
    {
    }

    public void OnUpdate(MeteorController meteor)
    {
    }
}

