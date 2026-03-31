using UnityEngine;

public interface IMeteorBehavior
{
    // 스폰될 때 실행 (코루틴 시작, 스탯 변형 등)
    void OnInit(MeteorController meteor);

    // 매 프레임 실행 (특별한 이동 기믹 등, 필요 없으면 비워둠)
    void OnUpdate(MeteorController meteor);

    // 죽을 때 실행 (파편 흩뿌리기, 장판 남기기 등)
    void OnDie(MeteorController meteor);

    // 오브젝트 풀로 돌아갈 때 실행 (코루틴 강제 정지, 색상 원상복구 등)
    void OnRelease(MeteorController meteor);
}
