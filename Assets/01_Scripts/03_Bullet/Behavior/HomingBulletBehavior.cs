using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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
        // 1. 안전하게 다운캐스팅해서 유도탄 스탯을 빼옵니다.
        if (!(bullet.Stat is HomingBulletStat homingStat)) return null;

        LayerMask targetLayer = LayerMask.GetMask("Meteor");
        // 2. transform.position 대신 bullet.transform.position 사용!

        float homingRange = Managers.Game._player.Stat.homingRange.TotalValue;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(bullet.transform.position, homingRange, targetLayer);

        MeteorController closestTarget = null;
        float minDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col == null || !col.gameObject.activeSelf) continue;

            MeteorController meteor = col.GetComponent<MeteorController>();
            if (meteor == null) continue;

            float dist = Vector2.Distance(bullet.transform.position, col.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestTarget = col.GetComponent<MeteorController>();
            }
        }

        return closestTarget;
    }

    private IEnumerator CoHomingRoutine(BulletController bullet)
    {
        MeteorController target = null;
        float turnSpeed = 200f;

        // bullet의 Stat에서 속도 등을 가져오기 위해 캐스팅
        if (!(bullet.Stat is HomingBulletStat homingStat)) yield break;

        // BulletController에 있는 Rigidbody2D 가져오기 (public 프로퍼티가 있다면 bullet.Rb 도 가능)
        Rigidbody2D rb = bullet.Rb;

        yield return new WaitForSeconds(0.2f);

        // gameObject 대신 bullet.gameObject 사용!
        while (bullet != null && bullet.gameObject.activeSelf)
        {
            if (target == null || !target.gameObject.activeSelf)
            {
                target = FindClosestTarget(bullet);
            }

            if (target != null)
            {
                Vector2 directionToTarget = ((Vector2)target.transform.position - (Vector2)bullet.transform.position).normalized;
                Vector2 currentVelocity = rb.linearVelocity;
                float angle = Vector2.SignedAngle(currentVelocity, directionToTarget);
                float rotateAmount = Mathf.Clamp(angle, -turnSpeed * Time.fixedDeltaTime, turnSpeed * Time.fixedDeltaTime);

                currentVelocity = Quaternion.Euler(0, 0, rotateAmount) * currentVelocity;

                rb.linearVelocity = currentVelocity.normalized * homingStat.speed.TotalValue;

                float faceAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0, 0, faceAngle - 90f);
            }

            yield return new WaitForFixedUpdate();
        }
    }
}


