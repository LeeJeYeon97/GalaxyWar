using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class PierceBulletBehavior : IBulletBehavior
{

    public void OnHit(BulletController bullet, GameObject target, BaseBulletStat activeStat)
    {
        if (bullet == null) return;

        Managers.Sound.Play(Define.SoundID.Sfx_PierceBullet_Hit);

        if (activeStat is PierceBulletStat stat)
        {
            //  1. 기획 데이터 가져오기 (예: 에디터에 20이 입력되어 있다면)
            float decreasePercent = stat.pierceDamageDecreaseValue.TotalValue;

            //  2. 백분율 배수 계산 
            // (100 - 20) / 100f = 0.8f (즉, 기존 데미지의 80%만 남게 됩니다)
            float multiplier = (100f - decreasePercent) / 100f;

            //  3. 최소 방어선 구축 (데미지가 0이 되거나 마이너스가 되는 것 원천 차단)
            // 아무리 많이 관통해도 최소 본래 데미지의 10%는 남도록 하한선을 둡니다.
            multiplier = Mathf.Clamp(multiplier, 0.7f, 1f);

            //  4. 최종 데미지 적용
            bullet.CurDamage = bullet.CurDamage * multiplier;

            // (선택 사항) 데미지가 소수점으로 너무 자잘하게 쪼개져서 1 미만으로 떨어지는 걸 막고 싶다면
            if (bullet.CurDamage < 1f)
            {
                bullet.CurDamage = 1f;
            }
        }
    }

    public void OnInit(BulletController bullet, BaseBulletStat activeStat)
    {
        if (activeStat is PierceBulletStat stat)
        {
            bullet.currentPierceCount = Mathf.FloorToInt(stat.pierceCount.TotalValue);
        }
    }

    public void OnRelease(BulletController bullet)
    {
    }

    public void OnShot(BulletController bullet)
    {
    }

    public void OnUpdate(BulletController bullet)
    {
    }
}