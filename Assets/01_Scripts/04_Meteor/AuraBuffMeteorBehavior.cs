using System;
using System.Collections;
using UnityEngine;
using static Define;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class AuraBuffMeteorBehavior : IMeteorBehavior
{
    public void OnDie(MeteorController meteor)
    {
    }

    public void OnInit(MeteorController meteor)
    {
        if (meteor.ActionCoroutine != null)
            Managers.Coroutine?.StopCoroutine(meteor.ActionCoroutine);

        meteor.ActionCoroutine = Managers.Coroutine.StartCoroutine(CoAuraPulse(meteor));
    }

    public void OnRelease(MeteorController meteor)
    {
        if (meteor.ActionCoroutine != null)
        {
            Managers.Coroutine?.StopCoroutine(meteor.ActionCoroutine);
            meteor.ActionCoroutine = null;
        }
    }

    public void OnUpdate(MeteorController meteor)
    {
    }
    private IEnumerator CoAuraPulse(MeteorController meteor)
    {
        while (true)
        {
            yield return new WaitForSeconds(0.2f);

            // 내 위치를 기준으로 auraRadius 반경 안의 모든 2D 콜라이더를 찾습니다!
            Collider2D[] colliders = Physics2D.OverlapCircleAll(meteor.transform.position, meteor.Stat.auraRadius.TotalValue);

            foreach (Collider2D col in colliders)
            {
                MeteorController otherMeteor = col.GetComponent<MeteorController>();

                // 나 자신은 제외하고, 다른 운석들에게만 0.3초짜리 버프를 쏴줍니다!
                if (otherMeteor != null && otherMeteor != meteor)
                {
                    otherMeteor.ReceiveAuraBuff(0.3f);
                }
            }
        }
    }
}

