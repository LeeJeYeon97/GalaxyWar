using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class SludgeMeteorBehavior : IMeteorBehavior
{
    public void OnDie(MeteorController meteor)
    {
        SludgePuddle puddle = Managers.Pool.Get<SludgePuddle>(Define.Pool.SludgePuddle);
        if (puddle != null)
        {
            puddle.Init(meteor.transform.position);
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
