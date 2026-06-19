using System.Collections;
using UnityEngine;
using DG.Tweening; // DOTween 필수

[CreateAssetMenu(fileName = "Pattern_Dash", menuName = "BossPatterns/Pattern_Dash")]
public class Pattern_DashSO : BossPatternSO
{
    [Header("돌진 패턴 설정")]
    public float warningTime = 1.0f; // 돌진 전 타겟을 응시하며 멈춰있는 시간 (경고)
    public float dashSpeed = 20f;    // 돌진하는 속도
    public float overshoot = 5f;     // 플레이어 위치를 뚫고 얼마나 더 지나갈 것인가 (여유 거리)

    public override IEnumerator Execute(BossController boss)
    {
        if (boss._isDead || Managers.Game._player == null) yield break;

        SpriteRenderer sr = boss.GetComponentInChildren<SpriteRenderer>();

        // 1. 경고 연출 먼저 시작 (이 시간 동안 플레이어는 도망갑니다)
        if (sr != null)
        {
            sr.DOColor(Color.red, warningTime).SetId(boss.gameObject);
        }

        yield return new WaitForGameTime(warningTime);

        // 💡 [수정된 핵심 포인트] 경고 시간이 끝난 '돌진 직전'에 플레이어의 현재 위치를 조준합니다!
        if (boss._isDead || Managers.Game._player == null) yield break; // 대기하는 동안 플레이어가 죽었을 수도 있으니 안전 검사

        Vector2 targetPos = Managers.Game._player.transform.position;
        Vector2 dashDir = (targetPos - (Vector2)boss.transform.position).normalized;

        // 2. 돌진 목표 지점 계산 (플레이어 위치를 관통)
        Vector2 endPos = targetPos + (dashDir * overshoot);

        float distance = Vector2.Distance(boss.transform.position, endPos);
        float duration = distance / dashSpeed;

        // 3. 콰쾅! 돌진 시작
        boss.transform.DOMove(endPos, duration).SetEase(Ease.InExpo).SetId(boss.gameObject);

        yield return new WaitForGameTime(duration);

        // 4. 돌진 종료 후 원래 색상 복귀
        if (sr != null)
        {
            sr.DOColor(Color.white, 0.2f).SetId(boss.gameObject);
        }

        // 5. 공통 후딜레이
        yield return new WaitForGameTime(nextPatternDelay);
    }
}