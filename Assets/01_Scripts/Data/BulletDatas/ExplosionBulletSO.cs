using DG.Tweening;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

[CreateAssetMenu(fileName = "ExplosionBullet", menuName = "Bullets/Explosion")]
public class ExplosionBulletSO : BulletDataSO
{
    [Header("Explosion Settings")]
    public float baseExplosionRange = 1f;
    public float baseExplosionDamage = 1f;
    
}
