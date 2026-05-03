using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;


public class FireBulletBehavior : IBulletBehavior
{

    public void OnHit(BulletController bullet, GameObject target)
    {
        if (target == null) return;

        MeteorController meteor = target.GetComponent<MeteorController>();

        if (meteor == null) return;

        float tickTime = 0.5f;

        Managers.Sound.Play(Define.SoundID.Sfx_FireBullet_Hit);

        if (bullet.Stat is FireBulletStat stat)
        {
            // 직접 맞았을 때의 화상 데미지 적용
            float totalBurnDamage = stat.damage.TotalValue * stat.fireDamageValue.TotalValue;

            float actualTickDamage = totalBurnDamage * tickTime;
            meteor.ApplyBurn(actualTickDamage, stat.fireRemainTime.TotalValue, tickTime);
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
        //if(bullet.Stat.curLevel >= 5)
        //{
        bullet.StartCoroutine(CoDropFireTrail(bullet));
        //}
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

            //  코루틴 방어 로직: 일시정지나 광고 중이면 계산을 아예 건너뛰고 멍때립니다.
            // (만약 Managers.Game.IsPaused 프로퍼티를 만드셨다면 if(Managers.Game.IsPaused) 로 쓰시면 훨씬 깔끔합니다!)
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                yield return null;
                continue;
            }

            // 이전에 깔았던 위치와 지금 총알의 위치 거리를 잽니다.
            if (Vector2.Distance(lastDropPos, bullet.transform.position) >= dropDistance)
            {
                // 지정한 거리만큼 멀어졌다면 장판을 소환!
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