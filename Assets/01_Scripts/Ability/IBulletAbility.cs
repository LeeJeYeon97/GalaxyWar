using DG.Tweening;
using GLTFast.Schema;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;


public struct AbilityExecuteParams
{
    public BulletStat stat;
    public BulletController bullet;
    public MeteorController meteor;
    public Collision2D collision; // 여기에 쿨리전 정보를 담습니다
    public Collider2D trigger;                   // .
    // 필요하다면 들어오는 방향 등도 미리 계산해서 넣을 수 있습니다.
    public Vector2 incomingDirection;
    public Vector2 shotDir;
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
        
    }
}
// 폭발탄
public class ExplosionBulletAbility : IBulletAbility
{
    private void ShowRange(AbilityExecuteParams param)
    {
        float finalRadius = param.stat.explosionRadius.TotalValue;
        float finalExplosionDmg = param.stat.damage.TotalValue;

        // --- 시각적 연출 (기존 로직 그대로 활용) ---
        GameObject indicator = Managers.Pool.Get<GameObject>(Define.Pool.ExplosionRangeIndicator);
        indicator.transform.position = param.collision.transform.position;
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
    }
    public void Execute(AbilityExecuteParams param)
    {
        Debug.Log("폭발탄 실행!");
        if (param.stat.type != Define.BulletType.ExplosionBullet ||
            param.stat.isActivated == false) return;

        float finalRadius = param.stat.explosionRadius.TotalValue;
        float finalExplosionDmg = param.stat.damage.TotalValue;

        ShowRange(param);

        // --- 실제 범위 데미지 로직 ---
        int layerMask = 1 << LayerMask.NameToLayer("Meteor");
        Collider2D[] colliders = Physics2D.OverlapCircleAll(param.collision.transform.position, finalRadius, layerMask);
        foreach (var col in colliders)
        {
            // 이미 맞은놈 제외
            if (col.gameObject == param.collision.gameObject) continue;

            MeteorController meteor = col.GetComponent<MeteorController>();
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
        if (param.bullet.canSplit == false)
        {
            return;
        }
        // 3. 반사 방향 계산 (튕겨나가는 기준 방향)
        // 충돌 지점의 Normal(법선)을 기준으로 들어온 방향을 반사시킵니다.
        Vector2 normal = param.collision.contacts[0].normal;
        Vector2 reflectDir = normal.normalized;

        // 4. 메테오 반지름 계산 (생성 위치 오프셋용)
        float meteorRadius = 0.5f;
        CircleCollider2D col = param.meteor.GetComponent<CircleCollider2D>();
        if (col != null)
        {
            meteorRadius = col.radius * param.meteor.transform.localScale.x;
        }

        // 5. 분열탄 생성 루프
        int totalCount = Mathf.RoundToInt(param.stat.splitCount.TotalValue);
        float spreadRange = 70f; // 부채꼴 퍼짐 각도

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
            BulletController splitBullet = Managers.Pool.Get<BulletController>(Define.Pool.SplitBullet);

            // 생성 위치: 메테오 중심 + (날아갈 방향 * 반지름 * 1.1f)
            // 이렇게 하면 메테오 표면 바로 밖에서 튀어나오는 연출이 됩니다.
            Vector3 spawnPos = param.meteor.transform.position + (Vector3)(spawnDir * (meteorRadius * 1.1f));
            splitBullet.transform.position = spawnPos;

            // 스탯 설정 및 분열탄 플래그 세팅
            splitBullet.SetBullet(Managers.Stat.GetBulletStat(Define.BulletType.SplitBullet));
            splitBullet.SetSplit();

            // 8. 발사!
            splitBullet.Shot(spawnDir);

            // (선택 사항) 생성된 분열탄이 방금 맞은 메테오와는 즉시 다시 부딪히지 않게 설정
            //Physics2D.IgnoreCollision(splitBullet.GetComponent<Collider2D>(), param.collision.collider, true);
        }
    }
}

// 번개탄
public class LightningBulletAbility : IBulletAbility
{
    public void Execute(AbilityExecuteParams param)
    {
        // 번개탄
        Debug.Log("번개탄 실행!");
        // 분열탄 활성화 안되어있으면 리턴
        if (param.stat.type != Define.BulletType.LightningBullet||
            param.stat.isActivated == false) return;

        Vector3 hitPos = param.meteor.transform.position;

        // 이미 번개에 맞은 적들을 저장 (중복 타격 방지)
        HashSet<GameObject> visitedTargets = new HashSet<GameObject>();
        visitedTargets.Add(param.meteor.gameObject);

        // 2. 주변 적들 탐색 (OverlapCircle)
        LayerMask targetLayer = LayerMask.GetMask("Meteor");
        int count = Mathf.FloorToInt(param.stat.lightningCount.TotalValue);

        for(int i = 0; i < count; i++)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(hitPos, param.stat.lightningRange.TotalValue, targetLayer);
            Collider2D closestEnemy = null;
            float minDistance = float.MaxValue;

            // 3. 가장 가까운 *다른* 적 찾기
            foreach (var col in colliders)
            {
                if (visitedTargets.Contains(col.gameObject)) continue;

                // 방금 맞은 놈은 제외
                if (col.gameObject == param.meteor.gameObject) continue;

                float dist = Vector2.Distance(hitPos, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestEnemy = col;
                }
            }

            // 4. 튕길 적이 있다면? 전기 지지기!
            if (closestEnemy != null)
            {
                // (1) 데미지 주기
                MeteorController nextTarget = closestEnemy.GetComponent<MeteorController>();
                if (nextTarget != null)
                {
                    // 예시 : 원본 데미지의 70%만 적용
                    // 전이될수록 데미지를 줄이고 싶다면 i를 활용 (예: 원본 * 0.8^i)
                    float chainDamage = param.stat.damage.TotalValue;
                    nextTarget.OnDamage(chainDamage);
                }

                // (2) 시각 효과 (번개 줄기 생성)
                GameObject effectGo = Managers.Pool.Get<GameObject>(Define.Pool.LightningEffect);
                LightningEffect effect = Util.GetOrAddComponent<LightningEffect>(effectGo);

                if (effect != null)
                {
                    effect.PlayEffect(hitPos, closestEnemy.transform.position);
                }
                hitPos = closestEnemy.transform.position;
                visitedTargets.Add(closestEnemy.gameObject);
            }
            else
            {
                // 주변에 더 이상 튈 적이 없으면 조기 종료
                break;
            }
        }
    }
}

// 관통탄
public class PierceBulletAbility : IBulletAbility
{
    public void Execute(AbilityExecuteParams param)
    {
        Debug.Log("관통탄 실행!");
        // 분열탄 활성화 안되어있으면 리턴
    }
}

public class BurstBulletAbility : IBulletAbility
{
    private List<IBulletAbility> _allAbilities = new List<IBulletAbility>();

    public BurstBulletAbility()
    {
        // 1. 모든 BulletType을 순회
        foreach (Define.BulletType type in Enum.GetValues(typeof(Define.BulletType)))
        {
            // 2. 무한 루프 방지를 위해 자기 자신(Burst)과 None은 제외
            if (type == Define.BulletType.BurstBullet)
                continue;

            // 3. 리플렉션으로 해당 타입의 Ability 인스턴스 생성
            string className = type.ToString() + "Ability";
            Type t = Type.GetType(className);

            if (t != null)
            {
                IBulletAbility ability = Activator.CreateInstance(t) as IBulletAbility;
                if (ability != null)
                    _allAbilities.Add(ability);
            }
        }
    }

    public void Execute(AbilityExecuteParams param)
    {
        Debug.Log("BurstBullet Execute");
        
        foreach (var ability in _allAbilities)
        {
            // 2. 어빌리티 이름에서 타입을 역추적 (예: ExplosionBulletAbility -> ExplosionBullet)
            Define.BulletType type = GetTypeFromAbility(ability);

            // 3. 매니저에서 해당 탄종의 '최신 스탯'을 가져옴
            BulletStat stat = Managers.Stat.GetBulletStat(type);

            if (stat != null && stat.isActivated)
            {
                // 4. 파라미터 복사본을 만들어 스탯을 '마스터 스탯'으로 교체
                AbilityExecuteParams burstParam = param;
                burstParam.stat = stat;
                //burstParam.isBurst = true;

                // 5. 실행! 이제 각 능력은 자신에게 맞는 최강의 수치로 작동함
                ability.Execute(burstParam);
            }
        }
    }

    private Define.BulletType GetTypeFromAbility(IBulletAbility ability)
    {
        string typeName = ability.GetType().Name.Replace("Ability", "");
        if (Enum.TryParse(typeName, out Define.BulletType type)) return type;
        return Define.BulletType.NormalBullet;
    }
}
