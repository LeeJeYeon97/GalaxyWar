using UnityEngine;

public class PlayerStat
{
    public Stat speed = new Stat();

    public Stat maxHp = new Stat();
    public Stat defence = new Stat();

    public Stat reloadCount = new Stat();
    public Stat reloadTime = new Stat();

    public Stat shotRange = new Stat();
    public Stat shotTime = new Stat();

    public float currentHp;
    public void SetStat(PlayerStatDataSO data)
    {
        if(data == null)
        {
            return;
        }

        speed.Init(data.speed);
        maxHp.Init(data.maxHp);
        currentHp = maxHp.TotalValue;
        defence.Init(data.defence);
        reloadCount.Init(data.reloadCount);
        reloadTime.Init(data.reloadTime);
        shotRange.Init(data.shotRange);
        shotTime.Init(data.shotTime);
    }

}
