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
    public void OnHit(BulletController bullet, GameObject target, BaseBulletStat activeStat)
    {
        Managers.Sound.Play(Define.SoundID.Sfx_homing_Hit);

    }
    public void OnInit(BulletController bullet, BaseBulletStat activeStat)    {    }

    public void OnRelease(BulletController bullet)    {    }

    public void OnUpdate(BulletController bullet) { }
    public void OnShot(BulletController bullet)
    {
        // 저장할 필요 없이 그냥 실행만 시켜줍니다.
        bullet.StartCoroutine(CoHomingRoutine(bullet));
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
            if (m != null && m.Movement._hasEnteredView == true)
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
        //  [핵심] UI 변수를 코루틴 내부의 '지역 변수'로 선언합니다.
        // 이 코루틴은 유도탄마다 별도로 돌아가므로, localUI는 각자 자기 것만 기억합니다.
        TargettingUI localUI = null;
        MeteorController target = null;
        bool isLocked = false;

        if (!(bullet.Stat is HomingBulletStat homingStat)) yield break;
        Rigidbody2D rb = bullet.Rb;

        // 1. 타겟 탐색 및 UI 생성
        target = FindClosestTarget(bullet);

        if (target != null)
        {
            isLocked = true;
            GameObject uiObj = Managers.Resource.Instantiate("Object/TargettingUI");
            if (uiObj != null)
            {
                localUI = uiObj.GetComponent<TargettingUI>();
                localUI.Show(target.transform, bullet);
                Managers.Sound.Play(Define.SoundID.Sfx_homingTargeting);
            }

            Vector2 directionToTarget = ((Vector2)target.transform.position - (Vector2)bullet.transform.position).normalized;
            rb.linearVelocity = directionToTarget * homingStat.speed.TotalValue;

            float faceAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, faceAngle - 90f);
        }

        while (bullet != null && bullet.gameObject.activeSelf)
        {
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                yield return null;
                continue;
            }

            if (isLocked && target != null && target.gameObject.activeSelf)
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
            else if (isLocked)
            {
                // 타겟이 죽었다면 락온 해제하고 UI 정리 (지역 변수라 안전함)
                isLocked = false;
                if (localUI != null) { localUI.Hide(); localUI = null; }
            }

            yield return new WaitForFixedUpdate();
        }
    }
}


