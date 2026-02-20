using UnityEngine;

public class PlayerStat
{
    public Stat speed = new Stat();

    public Stat maxHp = new Stat();
    public Stat maxDefence = new Stat();

    public Stat reloadCount = new Stat();
    public Stat reloadTime = new Stat();

    public Stat shotRange = new Stat();
    public Stat shotTime = new Stat();

    public bool enableBurst = false;
    public Stat maxBurstGuage = new Stat();
    public Stat maxBurstFullChargeTime = new Stat();

    public void SetStat(PlayerStatDataSO data)
    {
        if(data == null)
        {
            return;
        }

        speed.Init(data.speed);
        maxHp.Init(data.maxHp);
        maxDefence.Init(data.maxDefence);
        reloadCount.Init(data.reloadCount);
        reloadTime.Init(data.reloadTime);
        shotRange.Init(data.shotRange);
        shotTime.Init(data.shotTime);

        enableBurst = false;
        maxBurstGuage.Init(data.maxBurstGuage);
        maxBurstFullChargeTime.Init(data.maxBurstFullChargeTime);

    }

}
