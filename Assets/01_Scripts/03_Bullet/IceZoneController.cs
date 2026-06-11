using System.Collections;
using UnityEngine;
using static Define;

public class IceZoneController : MonoBehaviour
{
    private float _damage;
    [SerializeField]
    private float _radius = 2.0f;
    private float _slowPercent;
    private float _duration = 2.8f; // 장판 유지 및 폭발 대기 시간 (2.5초)

    // NonAlloc 최적화 바구니
    // 1. static 제거: 여러 장판이 겹쳤을 때 바구니(배열)를 공유하다 꼬이는 현상 방지
    private ContactFilter2D _filter;
    private readonly Collider2D[] _colliders = new Collider2D[30];
    private bool _isFilterInit = false;

    // 2. GC Alloc 최적화: 매번 new를 쓰지 않도록 시간을 미리 만들어둘 변수
    private WaitForSeconds _tickWait;

    public void Init(float damage, float radius, float slowPercent)
    {
        _damage = damage;
        _radius = radius;
        _slowPercent = slowPercent;

        if (!_isFilterInit)
        {
            _filter = new ContactFilter2D();

            _filter.useLayerMask = true;
            _filter.layerMask = LayerMask.GetMask("Meteor", "Boss");
            //  [핵심 추가] 이 필터가 트리거 콜라이더(Is Trigger)도 감지하도록 허용합니다!
            _filter.useTriggers = true;
            _isFilterInit = true;
        }

        // 3. 0.2초 기다리는 객체를 시작할 때 '딱 한 번만' 만들어 둡니다.
        _tickWait = new WaitForSeconds(0.2f);

        StartCoroutine(CoIceZoneRoutine());
    }

    private IEnumerator CoIceZoneRoutine()
    {
        float elapsed = 0f;
        float tickTime = 0.2f;

        // 1. 2.5초 동안 장판 유지 및 슬로우 부여
        while (elapsed < _duration)
        {
            if (Managers.Game.currentGameState != GameState.Pause)
            {
                int hitCount = Physics2D.OverlapCircle(transform.position, _radius, _filter, _colliders);
                for (int i = 0; i < hitCount; i++)
                {
                    if (_colliders[i].TryGetComponent(out MeteorController meteor))
                    {
                        // 0.3초짜리 짧은 슬로우를 지속적으로 부여 (장판 밖으로 나가면 곧바로 풀리게 됨)
                        meteor.Status.ApplySlow(_slowPercent, 0.3f);
                    }
                }
                elapsed += tickTime;
                yield return _tickWait;
            }
            else
            {
                yield return null;
            }
        }

        // 2. 2.5초 뒤 폭발! (데미지 처리)
        //Managers.Sound.Play(Define.SoundID.Sfx_IceShatter); // 쨍그랑! 터지는 사운드
        int finalHitCount = Physics2D.OverlapCircle(transform.position, _radius, _filter, _colliders);
        for (int i = 0; i < finalHitCount; i++)
        {
            if (_colliders[i].TryGetComponent(out MeteorController meteor))
            {
                meteor.OnDamage(_damage);
                Debug.Log($"얼음장판이 데미지 줬어요: {_damage}");
            }
        }

        // 3. 장판 파괴
        //Managers.Resource.Destroy(gameObject);
    }

    // =======================================================
    // 씬 뷰(Scene View)에서 범위를 확인하기 위한 기즈모 추가
    // =======================================================
    private void OnDrawGizmosSelected()
    {
        // 1. 얼음 느낌의 하늘색으로 선 색상 지정
        Gizmos.color = Color.cyan;

        // 2. 현재 위치(transform.position)를 기준으로 _radius만큼의 원을 그립니다.
        // Physics2D.OverlapCircle이 검사하는 완벽하게 똑같은 범위를 보여줍니다.
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}