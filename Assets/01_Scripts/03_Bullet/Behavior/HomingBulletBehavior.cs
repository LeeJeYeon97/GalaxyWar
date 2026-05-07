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

    public void OnInit(BulletController bullet, BaseBulletStat activeStat)
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
        MeteorController target = null;
        bool isLocked = false;

        // bullet의 Stat에서 속도 등을 가져오기 위해 캐스팅
        if (!(bullet.Stat is HomingBulletStat homingStat)) yield break;

        // BulletController에 있는 Rigidbody2D 가져오기
        Rigidbody2D rb = bullet.Rb;

        // =========================================================
        // 핵심 변경 1: 기존의 0.2초 대기(WaitForSeconds) 삭제
        // 생성되자마자 즉시 타겟을 찾습니다.
        // =========================================================
        target = FindClosestTarget(bullet); // 이Behavior 안에 있는 함수

        // =========================================================
        // 핵심 변경 2: 타겟이 있으면 즉시 그 방향으로 초기 속도 설정
        // =========================================================
        if (target != null)
        {
            isLocked = true;

            // 타겟을 향한 초기 방향 계산
            Vector2 directionToTarget = ((Vector2)target.transform.position - (Vector2)bullet.transform.position).normalized;

            // Rigidbody 속도 즉시 설정 (HomingShot에서 준 기본 방향 무시)
            rb.linearVelocity = directionToTarget * homingStat.speed.TotalValue;

            // 이미지 회전 즉시 설정
            float faceAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, faceAngle - 90f);
        }
        else
        {
            // 주변에 적이 아예 없으면 Shot()에서 준 기본 방향으로 날아갑니다.
        }

        while (bullet != null && bullet.gameObject.activeSelf)
        {
            // 코루틴 방어 로직: 일시정지 중이면 대기
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                yield return null;
                continue;
            }

            // =========================================================
            // 2. 유도 로직: 타겟이 '살아있을 때만' 방향을 꺾으며 쫓아갑니다.
            // =========================================================
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

            yield return new WaitForFixedUpdate();
        }
    }
}


