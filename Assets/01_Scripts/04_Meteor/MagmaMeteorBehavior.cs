using System.Collections;
using UnityEngine;

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
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            if (Managers.Game.currentGameState == Define.GameState.Playing)
            {
                MagmaPuddle puddle = Managers.Pool.Get<MagmaPuddle>(Define.Pool.MagmaPuddle);
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
