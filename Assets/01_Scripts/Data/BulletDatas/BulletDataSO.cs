using UnityEngine;

// 이 클래스는 직접 에셋으로 만들 수 없도록 abstract로 선언합니다.
public abstract class BulletDataSO : ScriptableObject
{
    [Header("Common Settings")]
    public Define.BulletType type;
    public string bulletName;
    public Sprite bulletColor;

    public float chance;    // 해당 불릿이 장전될 확률
    public float damage;    // 기본 데미지
    public float speed;     // 기본 속도
    public float hp;        // 기본 체력
    public bool isActivated;    // 활성화 여부
}