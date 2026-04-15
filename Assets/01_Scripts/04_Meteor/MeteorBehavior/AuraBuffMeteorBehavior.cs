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
        //  1. 생명주기 안전장치: while(true) 대신 운석이 살아있을 때만 돌게 합니다.
        while (meteor != null && meteor.gameObject.activeInHierarchy)
        {
            //  2. 마법의 타이머 적용: 일시정지면 알아서 시간이 멈춥니다.
            yield return new WaitForGameTime(0.2f);

            // 3. 이중 방어막: 대기가 막 끝난 찰나에 팝업이 떴을 수도 있으니 검사!
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                continue; // 버프를 뿌리지 않고 루프 처음으로 돌아갑니다.
            }

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

