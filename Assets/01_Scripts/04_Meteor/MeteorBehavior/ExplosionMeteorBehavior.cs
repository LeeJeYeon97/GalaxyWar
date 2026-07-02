using DG.Tweening;
using System.Collections;
using UnityEngine;

public class ExplosionMeteorBehavior : IMeteorBehavior
{
    public void OnInit(MeteorController meteor)
    {
        Transform indicator = meteor.transform.Find("ExplosionIndicator");
        if (indicator != null)
        {
            SpriteRenderer indicatorSr = indicator.GetComponent<SpriteRenderer>();
            if (indicatorSr != null)
            {
                // 초기 투명도를 안보이게 세팅
                Color c = indicatorSr.color;
                c.a = 0.0f;
                indicatorSr.color = c;
            }
        }
    }

    public void OnUpdate(MeteorController meteor)
    {
        // 플레이어가 없거나 메테오가 죽었다면 무시
        if (Managers.Game._player == null) return;

        // 아직 멈추지 않고 날아가는 중일 때만 거리 체크
        if (!meteor.Movement._isStopped)
        {
            // 플레이어와 메테오 사이의 거리 계산
            float distance = Vector2.Distance(meteor.transform.position, Managers.Game._player.transform.position);

            // 정해진 거리(3.5f) 이내로 들어왔다면?
            if (distance <= meteor.Stat.explosionTargetRadius.TotalValue)
            {
                // 1. 멈춤 상태로 변경 (더 이상 거리 체크를 하지 않도록)
                meteor.Movement._isStopped = true;

                // 2. 대표님 Movement의 속도를 0으로 만들고 리지드바디에 즉시 적용!
                if (meteor.Movement != null)
                {
                    meteor.Movement.currentSpeed.Init(0f);
                    meteor.Movement.UpdateVelocity();
                }

                // 멈추는 순간: 삐삐삑 빨간색 점등 연출 시작!
                SpriteRenderer sr = meteor.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    // 0.15초마다 빨간색으로 변했다가 돌아오기를 무한 반복 (-1)
                    // SetId를 통해 나중에 이 연출만 딱 짚어서 끌 수 있게 이름을 붙여둡니다.
                    sr.DOColor(Color.red, 0.15f).SetLoops(-1, LoopType.Yoyo).SetId(meteor.gameObject);
                }
                // 4.  [핵심] 메테오 본체에게 2초 뒤 폭발하는 코루틴을 넘겨줍니다!
                if (meteor.ActionCoroutine != null)
                {
                    meteor.StopCoroutine(meteor.ActionCoroutine);
                }
                meteor.ActionCoroutine = meteor.StartCoroutine(CoExplosionCountdown(meteor));
            }
        }
        
    }
    //  메테오 본체(MonoBehaviour)가 직접 실행해 줄 카운트다운 코루틴
    private IEnumerator CoExplosionCountdown(MeteorController meteor)
    {
        // 1. 가이드라인 오브젝트 찾기
        Transform indicator = meteor.transform.Find("ExplosionIndicator");
        if (indicator != null)
        {
            indicator.gameObject.SetActive(true);

            //  변경점 1: 스케일은 처음부터 최종 폭발 크기로 빵! 고정해버립니다.
            float targetRadius = meteor.Stat.explosionRadius.TotalValue;

            // [수정된 부분] 부모(메테오)의 스케일이 1이 아닐 경우를 대비해 나누어 줍니다.
            // 지름 = targetRadius * 2
            float exactScaleX = (targetRadius * 2f) / meteor.transform.localScale.x;
            float exactScaleY = (targetRadius * 2f) / meteor.transform.localScale.y;

            //  지름(2 * 반지름)만큼 스케일을 키워야 반지름 targetRadius와 일치합니다.
            indicator.localScale = new Vector3(exactScaleX, exactScaleY, 1f);

            SpriteRenderer indicatorSr = indicator.GetComponent<SpriteRenderer>();
            if (indicatorSr != null)
            {
                // 초기 투명도를 아주 옅게(예: 10%) 세팅
                Color c = indicatorSr.color;
                c.a = 0.1f;
                indicatorSr.color = c;

                //  변경점 2: DOFade를 사용해 0.2초마다 투명도를 10% <-> 60% 사이로 무한 깜빡임!
                // SetId를 메테오 본체로 맞춰두어, 나중에 OnDie에서 한 번에 싹 꺼지게 만듭니다.
                indicatorSr.DOFade(0.6f, 0.2f).SetLoops(-1, LoopType.Yoyo).SetId(meteor.gameObject);
            }
        }

        Managers.Sound.Play(Define.SoundID.Sfx_ExplosionMeteorAlaram);

        // 2초 대기 (이 시간 동안 계속 깜빡거립니다)
        yield return new WaitForSeconds(meteor.Stat.explosionDelay.TotalValue);

        indicator.gameObject.SetActive(false);
        // 살아있다면 폭발!
        if (meteor != null)
        {
            Explode(meteor);
        }
    }

    private void Explode(MeteorController meteor)
    {
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(meteor.transform.position, meteor.Stat.explosionRadius.TotalValue);
        foreach (Collider2D target in hitTargets)
        {
            if (target.CompareTag("Player"))
            {
                PlayerController player = target.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    float damage = meteor.Stat.Damage.TotalValue * 2f;
                    player.OnDamage(damage);

                    // [핵심] 콜라이더 중복 히트를 막기 위해 한 번 때렸으면 즉시 반복문을 탈출합니다!
                    break;
                }
            }
        }

        Managers.Effect.Play(Define.EffectType.MeteorExplosion, meteor.transform.position);
        Managers.Sound.Play(Define.SoundID.Sfx_ExplosionMeteor);
        meteor.OnDamage(9999f);
    }

    public void OnDie(MeteorController meteor)
    {
        CleanUp(meteor);
    }

    public void OnRelease(MeteorController meteor)
    {
        CleanUp(meteor);
    }

    //  사망하거나 풀에 들어갈 때 연출과 코루틴을 싹 청소합니다.
    private void CleanUp(MeteorController meteor)
    {
        DOTween.Kill(meteor.gameObject);

        if (meteor.ActionCoroutine != null)
        {
            meteor.StopCoroutine(meteor.ActionCoroutine);
            meteor.ActionCoroutine = null;
        }
    }


}