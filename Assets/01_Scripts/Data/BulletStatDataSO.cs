using UnityEngine;

[CreateAssetMenu(fileName = "BulletData", menuName = "ScriptableObjects/BulletData")]
public class BulletStatDataSO : ScriptableObject
{
    [Header("Common Settings")]
    public Define.BulletType type;
    public string bulletName;
    
    [Header("Base Stat")]
    public float chance;                // 해당 불릿이 장전될 확률
    public float damage;                // 기본 데미지
    public float speed;                 // 기본 속도
    public float bounceCount;           // 튕기는 횟수
    public bool isActivated;            // 활성화 여부

    [Header("Explosion Stat Settings")]
    public float baseExplosionRange;    // 기본 폭발 범위
    public float baseExplosionDamage;   // 기본 폭발 데미지

    [Header("Split Stat Settings")]
    public int baseSplitCount;          // 기본 스플릿 카운트
    public float baseSplitBulletDamage; // 기본 스플릿 탄의 데미지

    //[Header("Lighting Stat Settings")]


}