using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class FireBulletBehavior : IBulletBehavior
{

    public void OnHit(BulletController bullet, GameObject target)
    {
        if (target == null) return;

        MeteorController meteor = target.GetComponent<MeteorController>();

        if (meteor == null) return;

        if (bullet.Stat is FireBulletStat stat)
        {
            // 직접 맞았을 때의 화상 데미지 적용
            float totalBurnDamage = stat.damage.TotalValue * stat.fireDamageValue.TotalValue;
            meteor.ApplyBurn(totalBurnDamage, stat.fireRemainTime.TotalValue , stat.fireTickTime.TotalValue);
        }
    }

    public void OnInit(BulletController bullet)
    {
    }

    public void OnRelease(BulletController bullet)
    {
    }

    public void OnShot(BulletController bullet)
    {
        bullet.StartCoroutine(CoDropFireTrail(bullet));
    }

    public void OnUpdate(BulletController bullet)
    {
    }

    // =======================================================
    // 장판 생성 코루틴
    // =======================================================
    private IEnumerator CoDropFireTrail(BulletController bullet)
    {
        // 1. 발사된 초기 위치를 기록합니다.
        Vector2 lastDropPos = bullet.transform.position;

        // 2. 장판을 깔 간격 (필요하다면 나중에 FireBulletStat으로 빼셔도 좋습니다!)
        float dropDistance = 0.5f;
        
        // 3. 총알이 화면에 살아있는 동안 계속 감시합니다.
        while (bullet != null && bullet.gameObject.activeSelf)
        {
            // 이전에 깔았던 위치와 지금 총알의 위치 거리를 잽니다.
            if (Vector2.Distance(lastDropPos, bullet.transform.position) >= dropDistance)
            {
                // ★ 지정한 거리만큼 멀어졌다면 장판을 소환!
                // (유저님의 Pool 매니저가 문자열을 받는지, 프리팹을 받는지에 따라 수정해 주세요)
                GameObject fireZoneGo = Managers.Resource.Instantiate("Bullets/FirePuddle");

                if (fireZoneGo != null)
                {
                    fireZoneGo.transform.position = bullet.transform.position;

                    // (선택 사항) 장판 스크립트에 데미지를 전달해주면 더 완벽합니다!
                    FireZoneController zone = fireZoneGo.GetComponent<FireZoneController>();
                    if (bullet.Stat is FireBulletStat stat) 
                            zone.Init(stat);
                }

                // 다음 측정을 위해 현재 위치를 갱신합니다.
                lastDropPos = bullet.transform.position;
            }

            // 매 프레임마다 검사합니다.
            yield return null;
        }
    }
}