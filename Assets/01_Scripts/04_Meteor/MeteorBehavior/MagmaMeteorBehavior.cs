using System.Collections;
using UnityEngine;
using static Define;

public class MagmaMeteorBehavior : IMeteorBehavior
{
    public void OnInit(MeteorController meteor)
    {
        // 1. 혹시라도 예전에 돌던 코루틴이 남아있다면 끕니다.
        if (meteor.ActionCoroutine != null)
        {
            meteor.StopCoroutine(meteor.ActionCoroutine);
        }

        // 2. 핵심! 매니저가 아닌 'meteor' 본체에게 코루틴 실행을 맡깁니다.
        meteor.ActionCoroutine = meteor.StartCoroutine(CoDropMagma(meteor));
    }
    private IEnumerator CoDropMagma(MeteorController meteor)
    {
        //  1. 생명주기 안전장치: 운석이 씬에 살아있을 때만 무한 반복합니다.
        while (meteor != null && meteor.gameObject.activeInHierarchy)
        {
            //  2. 마법의 타이머: 게임이 일시정지되면 0.5초 타이머도 알아서 멈춥니다.
            yield return new WaitForGameTime(0.5f);

            //  3. 이중 방어막: 타이머가 끝난 찰나에 팝업이 떠서 멈췄다면 장판 생성을 스킵!
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                continue;
            }

            // 안전 검사 통과 시 마그마 소환!
            GameObject magma = meteor.Stat.magmaPuddle;
            GameObject go = Managers.Resource.Instantiate(magma);
            if (go != null)
            {
                MagmaPuddle puddle = go.GetComponent<MagmaPuddle>();
                if (puddle != null)
                {
                    float puddleDamage = meteor.Stat.Damage.TotalValue * 0.5f;
                    puddle.Init(meteor.transform.position, puddleDamage);
                }
            }
        }
    }

    public void OnUpdate(MeteorController meteor) { }
    public void OnDie(MeteorController meteor) { }

    public void OnRelease(MeteorController meteor)
    {
        // 풀에 반환되거나 비활성화될 때 코루틴을 안전하게 정지합니다.
        if (meteor.ActionCoroutine != null)
        {
            meteor.StopCoroutine(meteor.ActionCoroutine);
            meteor.ActionCoroutine = null;
        }
    }

}
