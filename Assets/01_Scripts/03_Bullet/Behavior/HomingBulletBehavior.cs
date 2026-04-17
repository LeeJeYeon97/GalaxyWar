using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;
public class HomingBulletBehavior : IBulletBehavior
{
    public void OnHit(BulletController bullet, GameObject target)
    {
    }

    public void OnInit(BulletController bullet)
    {
    }

    public void OnRelease(BulletController bullet)
    {
        //if (_homingCoroutine != null)
        //{
        //    bullet.StopCoroutine(_homingCoroutine);
        //    _homingCoroutine = null;
        //}
    }

    public void OnShot(BulletController bullet)
    {
        // 저장할 필요 없이 그냥 실행만 시켜줍니다.
        bullet.StartCoroutine(CoHomingRoutine(bullet));
    }

    public void OnUpdate(BulletController bullet)
    {
    }

    private MeteorController FindClosestTarget(BulletController bullet)
    {
        var meteors = Managers.Game.activeMeteors;

        if (meteors == null || meteors.Count == 0) return null;

        // 2. 유효한(활성화된) 적들만 따로 필터링 (화면 안의 적만 고르고 싶을 때 유리)
        // Linq를 쓰면 가독성이 좋지만, 모바일 최적화를 위해 단순 리스트 추출을 권장합니다.
        List<MeteorController> validMeteors = new List<MeteorController>();
        foreach (var m in meteors)
        {
            if (m != null && m._hasEnteredView == true)
            {
                validMeteors.Add(m);
            }
        }

        if (validMeteors.Count == 0) return null;

        // 3. 필터링된 리스트에서 랜덤하게 인덱스 하나 추출
        int randomIndex = UnityEngine.Random.Range(0, validMeteors.Count);
        return validMeteors[randomIndex];

    }

    private IEnumerator CoHomingRoutine(BulletController bullet)
    {
        MeteorController target = null;
        
        // bullet의 Stat에서 속도 등을 가져오기 위해 캐스팅
        if (!(bullet.Stat is HomingBulletStat homingStat)) yield break;

        // BulletController에 있는 Rigidbody2D 가져오기 (public 프로퍼티가 있다면 bullet.Rb 도 가능)
        Rigidbody2D rb = bullet.Rb;

        yield return new WaitForSeconds(0.2f);

        // gameObject 대신 bullet.gameObject 사용!
        while (bullet != null && bullet.gameObject.activeSelf)
        {
            //  코루틴 방어 로직: 일시정지나 광고 중이면 계산을 아예 건너뛰고 멍때립니다.
            // (만약 Managers.Game.IsPaused 프로퍼티를 만드셨다면 if(Managers.Game.IsPaused) 로 쓰시면 훨씬 깔끔합니다!)
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                yield return null;
                continue;
            }

            if (target == null || !target.gameObject.activeSelf)
            {
                target = FindClosestTarget(bullet);
            }

            if (target != null)
            {
                Vector2 directionToTarget = ((Vector2)target.transform.position - (Vector2)bullet.transform.position).normalized;
                Vector2 currentVelocity = rb.linearVelocity;
                float angle = Vector2.SignedAngle(currentVelocity, directionToTarget);
                float rotateAmount = Mathf.Clamp(angle, -homingStat.turnSpeed * Time.fixedDeltaTime, homingStat.turnSpeed * Time.fixedDeltaTime);

                currentVelocity = Quaternion.Euler(0, 0, rotateAmount) * currentVelocity;

                rb.linearVelocity = currentVelocity.normalized * homingStat.speed.TotalValue;

                float faceAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0, 0, faceAngle - 90f);
            }

            yield return new WaitForFixedUpdate();
        }
    }
}


