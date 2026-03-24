using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public interface IBulletBehavior
{
    // 스폰될 때 실행 (코루틴 시작, 스탯 변형 등)
    void OnInit(BulletController bullet);

    // 발사 되었을 때 실행
    void OnShot(BulletController bullet);

    // 맞았을 때 실행
    void OnHit(BulletController bullet, GameObject target);

    // 매 프레임 실행 (특별한 이동 기믹 등, 필요 없으면 비워둠)
    void OnUpdate(BulletController bullet);

    // 오브젝트 풀로 돌아갈 때 실행 (코루틴 강제 정지, 색상 원상복구 등)
    void OnRelease(BulletController bullet);
}
