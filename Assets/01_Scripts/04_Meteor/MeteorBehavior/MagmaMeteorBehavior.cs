using System.Collections;
using UnityEngine;
using static Define;

public class MagmaMeteorBehavior : IMeteorBehavior
{
    public void OnInit(MeteorController meteor)
    {
        // 뇌 안에 코루틴을 저장하지 않고, 메테오 몸통의 변수에 저장시킵니다!
        if (meteor.ActionCoroutine != null)
            Managers.Coroutine?.StopCoroutine(meteor.ActionCoroutine);

        meteor.ActionCoroutine = Managers.Coroutine.StartCoroutine(CoDropMagma(meteor));
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
            GameObject go = Managers.Resource.Instantiate("Object/MagmaPuddle");
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

        // 몸통에 저장된 코루틴을 꺼줍니다.
        if (meteor.ActionCoroutine != null)
        {
            Managers.Coroutine?.StopCoroutine(meteor.ActionCoroutine);
            meteor.ActionCoroutine = null;
        }
    }

}
