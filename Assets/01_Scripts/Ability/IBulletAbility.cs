using DG.Tweening;
using UnityEngine;


public struct AbilityExecuteParams
{
    public BulletStat stat;
    public MeteorController target;
    public Collision2D collision; // 여기에 쿨리전 정보를 담습니다
                                  // .
    // 필요하다면 들어오는 방향 등도 미리 계산해서 넣을 수 있습니다.
    public Vector2 incomingDirection;
}

public interface IBulletAbility
{
    void Execute(AbilityExecuteParams param);
}

// 기본탄 
public class NormalBulletAbility : IBulletAbility
{
    public void Execute(AbilityExecuteParams param)
    {
        if (param.target == null)
        {
            return;
        }
        param.target.OnDamage(param.stat.damage.TotalValue);
    }
}
// 폭발탄
public class ExplosionBulletAbility : IBulletAbility
{
    public void Execute(AbilityExecuteParams param)
    {
        Debug.Log("폭발탄 실행!");
        if (param.stat.type != Define.BulletType.ExplosionBullet ||
            param.stat.isActivated == false) return;

        float finalRadius = param.stat.explosionRadius.TotalValue;
        float finalExplosionDmg = param.stat.damage.TotalValue;

        // --- 시각적 연출 (기존 로직 그대로 활용) ---
        GameObject indicator = Managers.Pool.Get<GameObject>(Define.Pool.ExplosionRangeIndicator);
        indicator.transform.position = param.target.transform.position;
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
        Collider2D[] colliders = Physics2D.OverlapCircleAll(param.target.transform.position, finalRadius, layerMask);
        foreach (var col in colliders)
        {
            MeteorController meteor = col.GetComponentInParent<MeteorController>();
            if(meteor)
            {
                meteor.OnDamage(finalExplosionDmg);
            }
        }
    }
}

// 분열탄
public class SplitBulletAbility : IBulletAbility
{
    public void Execute(AbilityExecuteParams param)
    {
        Debug.Log("분열탄 실행!");
        // 분열탄 활성화 안되어있으면 리턴
        if (param.stat.type != Define.BulletType.SplitBullet ||
            param.stat.isActivated == false) return;

        // 이미 분열된 애면
        if(param.stat.canSplit == true)
        {
            // 뎀지만 주기
            param.target.OnDamage(param.stat.damage.TotalValue);
            return;
        }

        // 3. 반사 방향 계산 (튕겨나가는 기준 방향)
        // 충돌 지점의 Normal(법선)을 기준으로 들어온 방향을 반사시킵니다.
        Vector2 normal = param.collision.contacts[0].normal;
        Vector2 reflectDir = normal.normalized;

        // 4. 메테오 반지름 계산 (생성 위치 오프셋용)
        float meteorRadius = 0.5f;
        CircleCollider2D col = param.target.GetComponent<CircleCollider2D>();
        if (col != null)
        {
            meteorRadius = col.radius * param.target.transform.localScale.x;
        }

        // 5. 분열탄 생성 루프
        int totalCount = Mathf.RoundToInt(param.stat.splitCount.TotalValue);
        float spreadRange = 70f; // 부채꼴 퍼짐 각도

        param.target.OnDamage(param.stat.damage.TotalValue);

        for (int i = 0; i < totalCount; i++)
        {
            float angleOffset = 0;
            if (totalCount > 1)
            {
                angleOffset = (i - (totalCount - 1) * 0.5f) * (spreadRange / (totalCount - 1));
            }

            // 6. 기준 반사 방향(reflectDir)에서 angleOffset만큼 회전
            Vector2 spawnDir = Quaternion.Euler(0, 0, angleOffset) * reflectDir;

            // 7. 풀에서 생성 및 설정
            BulletController splitBullet = Managers.Pool.Get<BulletController>(Define.Pool.Bullet);

            // 생성 위치: 메테오 중심 + (날아갈 방향 * 반지름 * 1.1f)
            // 이렇게 하면 메테오 표면 바로 밖에서 튀어나오는 연출이 됩니다.
            Vector3 spawnPos = param.target.transform.position + (Vector3)(spawnDir * (meteorRadius * 1.1f));
            splitBullet.transform.position = spawnPos;

            // 스탯 설정 및 분열탄 플래그 세팅
            splitBullet.SetBullet(Managers.Stat.GetBulletStat(Define.BulletType.SplitBullet));
            splitBullet.SetSplitBullet();

            // 8. 발사!
            splitBullet.Shot(spawnDir);

            // (선택 사항) 생성된 분열탄이 방금 맞은 메테오와는 즉시 다시 부딪히지 않게 설정
            //Physics2D.IgnoreCollision(splitBullet.GetComponent<Collider2D>(), param.collision.collider, true);
        }
    }
}
