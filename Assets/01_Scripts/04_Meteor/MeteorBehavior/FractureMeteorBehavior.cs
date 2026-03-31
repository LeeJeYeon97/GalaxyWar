using UnityEngine;
using System;
using static Define;

public class FractureMeteorBehavior : IMeteorBehavior
{
    public void OnDie(MeteorController meteor)
    {
        // 2개 ~ 4개의 파편을 흩뿌림
        int fragmentCount = UnityEngine.Random.Range(2, 5);

        for (int i = 0; i < fragmentCount; i++)
        {
            ////MeteorController fragment = Managers.Pool.Get(meteor.stat);
            //if (fragment != null)
            //{
            //    // 현재 죽은 위치에서, Fragment 타입으로, 사방으로 튀게 Init!
            //    fragment.Init(meteor.transform.position, Managers.Stat.GetMeteorStat(MeteorType.FragmentMeteor));
            //}
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

