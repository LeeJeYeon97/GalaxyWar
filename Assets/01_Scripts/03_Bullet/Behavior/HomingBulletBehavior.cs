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

    public void OnInit(BulletController bullet, BaseBulletStat activeStat) { }
    public void OnRelease(BulletController bullet) { }
    public void OnUpdate(BulletController bullet) { }

    public void OnShot(BulletController bullet)
    {
        bullet.StartCoroutine(CoHomingRoutine(bullet));
    }

    //  1. 특정 몬스터 리스트에 의존하지 않고 Physics 레이더를 사용해 타겟을 찾습니다.
    private Transform FindRandomTarget(BulletController bullet)
    {
        // 화면을 덮을 만큼 넉넉한 탐색 반경 (기획에 맞게 수치를 조절하세요!)
        float searchRadius = 30f;

        // 메테오와 보스 레이어를 모두 검색
        int layerMask = LayerMask.GetMask("Meteor", "Boss");
        Collider2D[] colliders = Physics2D.OverlapCircleAll(bullet.transform.position, searchRadius, layerMask);

        List<Transform> validTargets = new List<Transform>();

        foreach (var col in colliders)
        {
            //  2. 메테오인지 보스인지 묻지 않고 IDamageable 자격증만 확인!
            IDamageable damageable = col.GetComponent<IDamageable>();

            // 인터페이스가 존재하고, 현재 활성화된(살아있는) 오브젝트만 유효 타겟으로 판정
            // (기존의 _hasEnteredView 역할은 OverlapCircle 반경 안에 들어왔는지로 대체됩니다)
            if (damageable != null && col.gameObject.activeInHierarchy)
            {
                validTargets.Add(col.transform);
            }
        }

        if (validTargets.Count == 0) return null;

        // 3. 필터링된 리스트에서 랜덤하게 인덱스 하나 추출
        int randomIndex = UnityEngine.Random.Range(0, validTargets.Count);
        return validTargets[randomIndex];
    }

    private IEnumerator CoHomingRoutine(BulletController bullet)
    {
        if (!(bullet.Stat is HomingBulletStat homingStat)) yield break;
        Rigidbody2D rb = bullet.Rb;

        Transform target = FindRandomTarget(bullet);

        if (target != null)
        {
            GameObject uiObj = Managers.Resource.Instantiate("Object/TargettingUI");
            if (uiObj != null)
            {
                TargettingUI localUI = uiObj.GetComponent<TargettingUI>();

                // UI에게 타겟과 총알(자기 자신)을 넘겨주기만 하면 끝입니다! 
                // UI 끄는 건 이제 UI가 알아서 할 겁니다.
                localUI.Show(target, bullet);
                Managers.Sound.Play(Define.SoundID.Sfx_homingTargeting);
            }

            // 초기 방향 설정
            Vector2 directionToTarget = ((Vector2)target.position - (Vector2)bullet.transform.position).normalized;
            rb.linearVelocity = directionToTarget * homingStat.speed.TotalValue;

            float faceAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(0, 0, faceAngle - 90f);
        }

        // 총알이 살아있는 동안의 유도 로직만 남깁니다.
        while (bullet != null && bullet.gameObject.activeInHierarchy)
        {
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                yield return null;
                continue;
            }

            // 타겟이 살아있을 때만 방향을 틉니다.
            if (target != null && target.gameObject.activeInHierarchy)
            {
                Vector2 directionToTarget = ((Vector2)target.position - (Vector2)bullet.transform.position).normalized;
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


