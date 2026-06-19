using UnityEngine;

public class EnergyFieldController : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private LayerMask targetLayer; // 인스펙터에서 [Meteor, Boss] 레이어를 선택하세요.

    private float _damage;
    private float _damageInterval = 0.5f; // 0.5초마다 데미지
    private float _radius;
    private float _timer = 0f;

    // 수치 초기화 함수
    [Header("스케일 보정")]
    [SerializeField] private float radiusToScaleRatio = 12.5f; // 인스펙터에서 12.5로 입력

    public void Init(float damage, float radius, float interval)
    {
        _damage = damage;
        _damageInterval = interval;
        _radius = radius;

        // 인스펙터의 설정값을 사용하여 계산
        float calculatedScale = radius / radiusToScaleRatio;
        transform.localScale = new Vector3(calculatedScale, calculatedScale, 1f);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 1. 레이어 필터링 (Physics 2D 매트릭스 설정과 함께 이중 안전장치)
        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            // 2. IDamageable 인터페이스 확인
            if (collision.TryGetComponent(out IDamageable damageable))
            {
                _timer += Time.fixedDeltaTime;
                if (_timer >= _damageInterval)
                {
                    _timer = 0f;

                    float finalDamage = Managers.Game._player.Stat.damage.TotalValue * (_damage / 100f);
                    Managers.Sound.Play(Define.SoundID.Sfx_EnergyFieldHit);
                    Managers.Effect.Play(Define.EffectType.EnergyFieldHit, collision.transform.position);
                    // 3. 인터페이스의 메서드 호출)
                    damageable.OnDamage(finalDamage);
                }
            }
        }
    }
}