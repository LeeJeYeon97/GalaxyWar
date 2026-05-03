using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;


public struct AbilityExecuteParams
{
    public BaseBulletStat stat;
    public BulletController bullet;
    public MeteorController meteor;
    public Collision2D collision; // 여기에 쿨리전 정보를 담습니다
    public Collider2D trigger;                   
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
public class IceBulletAbility : IBulletAbility
{
    public void Execute(AbilityExecuteParams param)
    {

        
    }
}
// 분열탄
//public class SplitBulletAbility : IBulletAbility
//{
//    public void Execute(AbilityExecuteParams param)
//    {
//        Debug.Log("분열탄 실행!");
//         분열탄 활성화 안되어있으면 리턴
//        if (param.stat.type != Define.BulletType.SplitBullet ||
//            param.stat.isActivated == false) return;

//         이미 분열된 애면
//        if (param.bullet.canSplit == false)
//        {
//            return;
//        }
//         3. 반사 방향 계산 (튕겨나가는 기준 방향)
//         충돌 지점의 Normal(법선)을 기준으로 들어온 방향을 반사시킵니다.
//        Vector2 normal = Vector2.zero;
//        Vector2 reflectDir = Vector2.zero;
//        if (param.collision != null)
//        {
//            normal = param.collision.contacts[0].normal;
//            reflectDir = normal.normalized;
//        }
//        else if(param.trigger != null)
//        {
//             1. 상대방 콜라이더 위에서 내 위치와 가장 가까운 점을 찾음
//            Vector2 closestPoint = param.trigger.ClosestPoint(param.bullet.transform.position);
//             2. 내 위치에서 그 점을 빼면 노멀 방향이 나옴
//            normal = ((Vector2)param.bullet.transform.position - closestPoint).normalized;
//            Rigidbody2D rb = param.bullet.gameObject.GetComponent<Rigidbody2D>();
//            reflectDir = Vector2.Reflect(rb.linearVelocity.normalized, normal);
//        }

//         4. 메테오 반지름 계산 (생성 위치 오프셋용)
//        float meteorRadius = 0.5f;
//        CircleCollider2D col = param.meteor.GetComponent<CircleCollider2D>();
//        if (col != null)
//        {
//            meteorRadius = col.radius * param.meteor.transform.localScale.x;
//        }

//         5. 분열탄 생성 루프
//        int totalCount = Mathf.RoundToInt(param.stat.splitCount.TotalValue);
//        float spreadRange = 70f; // 부채꼴 퍼짐 각도

//        for (int i = 0; i < totalCount; i++)
//        {
//            float angleOffset = 0;
//            if (totalCount > 1)
//            {
//                angleOffset = (i - (totalCount - 1) * 0.5f) * (spreadRange / (totalCount - 1));
//            }

//             6. 기준 반사 방향(reflectDir)에서 angleOffset만큼 회전
//            Vector2 spawnDir = Quaternion.Euler(0, 0, angleOffset) * reflectDir;

//             7. 풀에서 생성 및 설정
//            BulletController splitBullet = Managers.Pool.Get(param.stat.originalPrefabs).GetComponent<BulletController>();

//             생성 위치: 메테오 중심 + (날아갈 방향 * 반지름 * 1.1f)
//             이렇게 하면 메테오 표면 바로 밖에서 튀어나오는 연출이 됩니다.
//            Vector3 spawnPos = param.meteor.transform.position + (Vector3)(spawnDir * (meteorRadius * 1.1f));
//            splitBullet.transform.position = spawnPos;

//             스탯 설정 및 분열탄 플래그 세팅
//            splitBullet.SetBullet(Managers.Stat.GetBulletStat(Define.BulletType.SplitBullet));
//            splitBullet.SetSplit();

//             8. 발사!
//            splitBullet.Shot(spawnDir,spawnPos);

//             (선택 사항) 생성된 분열탄이 방금 맞은 메테오와는 즉시 다시 부딪히지 않게 설정
//            Physics2D.IgnoreCollision(splitBullet.GetComponent<Collider2D>(), param.collision.collider, true);
//        }
//    }
//}


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
        //foreach (var ability in _allAbilities)
        //{
        //    // 2. 어빌리티 이름에서 타입을 역추적 (예: ExplosionBulletAbility -> ExplosionBullet)
        //    Define.BulletType type = GetTypeFromAbility(ability);
        //
        //    // 3. 매니저에서 해당 탄종의 '최신 스탯'을 가져옴
        //    BulletStat stat = Managers.Stat.GetBulletStat(type);
        //
        //    if (stat != null && stat.isActivated)
        //    {
        //        // 4. 파라미터 복사본을 만들어 스탯을 '마스터 스탯'으로 교체
        //        AbilityExecuteParams burstParam = param;
        //        burstParam.stat = stat;
        //        //burstParam.isBurst = true;
        //
        //        // 5. 실행! 이제 각 능력은 자신에게 맞는 최강의 수치로 작동함
        //        ability.Execute(burstParam);
        //    }
        //}
    }

    private Define.BulletType GetTypeFromAbility(IBulletAbility ability)
    {
        string typeName = ability.GetType().Name.Replace("Ability", "");
        if (Enum.TryParse(typeName, out Define.BulletType type)) return type;
        return Define.BulletType.NormalBullet;
    }
}
