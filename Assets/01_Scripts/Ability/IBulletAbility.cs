using DG.Tweening;
using UnityEngine;

public interface IBulletAbility
{
    void Execute(BulletStat stat, MeteorController target);
}

// 기본탄 
public class NormalBulletAbility : IBulletAbility
{
    public void Execute(BulletStat stat, MeteorController target)
    {
        if (target == null)
        {
            return;
        }

        target.OnDamage(stat.damage.TotalValue);
    }
}
// 폭발탄
public class ExplosionBulletAbility : IBulletAbility
{
    public void Execute(BulletStat stat, MeteorController target)
    {
        Debug.Log("폭발탄 실행!");
        if (stat.type != Define.BulletType.ExplosionBullet ||
            stat.isActivated == false) return;

        float finalRadius = stat.explosionRadius.TotalValue;
        float finalExplosionDmg = stat.explosionDamage.TotalValue;

        // --- 시각적 연출 (기존 로직 그대로 활용) ---
        GameObject indicator = Managers.Pool.Get<GameObject>(Define.Pool.ExplosionRangeIndicator);
        indicator.transform.position = target.transform.position;
        indicator.transform.localScale = Vector3.zero;

        SpriteRenderer sr = indicator.GetComponent<SpriteRenderer>();
        Sequence seq = DOTween.Sequence();
        seq.Append(indicator.transform.DOScale(Vector3.one * finalRadius * 2f, 0.2f).SetEase(Ease.OutQuad));
        seq.Join(sr.DOFade(0.5f, 0.1f));
        seq.Append(sr.DOFade(0f, 0.2f));
        seq.OnComplete(() =>
        {
            Managers.Pool.Release(indicator);
        });

        // --- 실제 범위 데미지 로직 ---
        int layerMask = 1 << LayerMask.NameToLayer("Meteor");
        Collider2D[] colliders = Physics2D.OverlapCircleAll(target.transform.position, finalRadius, layerMask);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent<MeteorController>(out MeteorController meteor))
            {
                // 맞은애 포함 운석들에게 폭발 데미지 전달
                meteor.OnDamage(finalExplosionDmg);
            }
        }
    }
}

// 분열탄
public class SplitAbility : IBulletAbility
{
    public void Execute(BulletStat stat, MeteorController target)
    {
        //// 분열탄 활성화 안되어있으면 리턴
        //if (stat.type != Define.BulletType.SplitBullet || 
        //    stat.isActivated == false) return;
        //
        //// 1. 확률 계산: 기본 사양 + 강화 수치
        ////float totalChance = stat.chance.TotalValue;
        ////
        ////if (Random.value > totalChance) return;
        ////
        ////Debug.Log("분열 완료!");
        //// 2. 최종 분열 개수 (float을 int로 반올림)
        //
        //int totalCount = Mathf.RoundToInt(stat.splitCount.TotalValue);
        //
        //// 2. 현재 공의 물리 정보 (진행 방향 및 속도)
        //Vector2 currentVelocity = _rb.linearVelocity;
        //float currentSpeed = currentVelocity.magnitude;
        //
        //// 공이 너무 느리거나 멈춰있을 경우를 대비해 최소 속도 보정
        //if (currentSpeed < 0.1f)
        //    currentSpeed = stat.speed.TotalValue;
        //
        //// 3. 부채꼴 각도 설정 (예: 전체 40도 범위 내에서 분산)
        //float spreadRange = 40f;
        //
        //for (int i = 0; i < totalCount; i++)
        //{
        //    // 4. 각도 계산 (가운데를 중심으로 골고루 배분)
        //    // i=0, 1 일 때 -> -20도, +20도
        //    // i=0, 1, 2 일 때 -> -20도, 0도, +20도
        //    float angleOffset = 0;
        //    if (totalCount > 1)
        //    {
        //        angleOffset = (i - (totalCount - 1) * 0.5f) * (spreadRange / (totalCount - 1));
        //    }
        //
        //    Vector2 spawnDir = RotateVector(currentVelocity.normalized, angleOffset);
        //
        //    // 5. 풀에서 분열탄 생성
        //    BulletController splitBullet = Managers.Pool.Get<BulletController>("Bullet");
        //
        //    // 현재 부딪힌 위치에서 생성
        //    splitBullet.transform.position = transform.position;
        //
        //    // 중요: 타입을 SplitBullet으로 넘겨서 재분열을 방지함 (_canSplit = false 로직 실행됨)
        //    //splitBullet.SetBullet(transform.position, BulletType.SplitBullet);
        //
        //    // 6. 물리 적용 (속도 부여)
        //    splitBullet.ShotWithVelocity(spawnDir * currentSpeed);
        //}
    }
}
