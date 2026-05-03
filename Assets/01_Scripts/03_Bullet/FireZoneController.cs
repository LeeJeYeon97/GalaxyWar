using JetBrains.Annotations;
using System.Collections;

using UnityEngine;
using UnityEngine.Rendering;

public class FireZoneController : MonoBehaviour
{
    private FireBulletStat _stat;
    private float _damage;

    public void Init(FireBulletStat stat)
    {
        _stat = stat;

        // 장판 자체의 공격력 계산 (총알 데미지 * 화염 배율)
        _damage = _stat.damage.TotalValue * _stat.fireDamageValue.TotalValue;

        // 일정 시간(화염 유지 시간) 이후에 사라지는 코루틴 시작
        //StopAllCoroutines();
        //StartCoroutine(CoFireZoneRelease(_stat.fireRemainTime.TotalValue));

        ParticleSystem ps = GetComponent<ParticleSystem>();

        // 2. 파티클의 '메인 모듈'에 접근합니다. (이 과정을 꼭 거쳐야 합니다!)
        ParticleSystem.MainModule mainModule = ps.main;

        // 3. Duration 값을 원하는 시간(초 단위)으로 설정합니다.
        mainModule.duration = _stat.fireZoneDestroyTime.TotalValue;

        ps.Play();
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 운석인지 확인
        MeteorController meteor = collision.gameObject.GetComponent<MeteorController>();

        if (meteor != null)
        {
            // 2. 이전에 만든 ApplyBurn 함수를 호출하여 화상 상태 이상 부여!
            // 데미지, 지속 시간, 틱 간격을 전달합니다.
            meteor.ApplyBurn(_damage, _stat.fireRemainTime.TotalValue, 0.5f);

            Debug.Log($"장판이 {collision.name}에게 화상을 입혔습니다!");
        }
    }

    // 장판이 스스로 사라지는 로직
    public IEnumerator CoFireZoneRelease(float duration)
    {
        // stat에 설정된 시간만큼 대기
        yield return new WaitForGameTime(duration);

        // 오브젝트 풀로 반납
        Managers.Resource.Destroy(this.gameObject);
    }
}
