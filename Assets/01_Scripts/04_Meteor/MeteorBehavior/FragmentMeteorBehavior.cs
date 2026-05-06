using System;
using UnityEngine;
public class FragmentMeteorBehavior : IMeteorBehavior
{
    public void OnDie(MeteorController meteor)
    {
    }

    public void OnInit(MeteorController meteor)
    {
        Vector2 scatterDir = UnityEngine.Random.insideUnitCircle.normalized;
        float speed = UnityEngine.Random.Range(meteor.Stat.MinSpeed.TotalValue, meteor.Stat.MaxSpeed.TotalValue);
        meteor.Movement._rb.linearVelocity = scatterDir * speed;
    }

    public void OnRelease(MeteorController meteor)
    {
    }

    public void OnUpdate(MeteorController meteor)
    {
    }
}
