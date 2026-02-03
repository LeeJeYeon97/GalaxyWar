using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatData", menuName = "ScriptableObjects/PlayerStatData")]
public class PlayerStatDataSO : ScriptableObject
{

    public float speed;

    public float maxHp;
    public float defence;

    public float reloadCount;
    public float reloadTime;

    public float shotRange;
    public float shotTime;

}
