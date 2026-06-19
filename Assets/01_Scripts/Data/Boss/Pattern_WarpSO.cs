using System.Collections;
using UnityEngine;
using DG.Tweening;

public enum WarpTargetType
{
    RandomScreen, // 화면 내 랜덤
    NearPlayer,   // 플레이어 근처로 기습
    Center        // 맵 정중앙으로 복귀
}

[CreateAssetMenu(fileName = "Pattern_Warp", menuName = "BossPatterns/Pattern_Warp")]
public class Pattern_WarpSO : BossPatternSO
{
    [Header("워프 패턴 설정")]
    public float fadeOutTime = 0.5f; // 사라지는 데 걸리는 시간
    public float fadeInTime = 0.5f;  // 나타나는 데 걸리는 시간
    public WarpTargetType warpType = WarpTargetType.NearPlayer;
    public float warpRadius = 4f;    // NearPlayer일 경우, 플레이어 주변 몇 거리 안으로 떨어질지

    public override IEnumerator Execute(BossController boss)
    {
        if (boss._isDead) yield break;

        SpriteRenderer sr = boss.GetComponentInChildren<SpriteRenderer>();
        Collider2D col = boss.GetComponent<Collider2D>();

        // 1. 사라지기 (Fade Out)
        if (col != null) col.enabled = false;

        if (sr != null)
        {
            sr.DOFade(0f, fadeOutTime).SetId(boss.gameObject);
        }

        yield return new WaitForGameTime(fadeOutTime);

        // [수정된 부분] 0부터 2까지의 숫자 중 하나를 랜덤으로 뽑아 Enum으로 변환합니다.
        // Random.Range에서 int를 사용할 때 최대값(exclusive)은 포함되지 않으므로 0, 1, 2가 나옵니다.
        WarpTargetType randomWarpType = (WarpTargetType)UnityEngine.Random.Range(0, 3);

        // 2. 위치 이동 계산
        Vector2 newPos = boss.transform.position;
        switch (randomWarpType)
        {
            case WarpTargetType.Center:
                newPos = Vector2.zero;
                break;
            case WarpTargetType.NearPlayer:
                if (Managers.Game._player != null)
                {
                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle.normalized * warpRadius;
                    newPos = (Vector2)Managers.Game._player.transform.position + randomOffset;
                }
                break;
            case WarpTargetType.RandomScreen:
                // 맵 범위 내 랜덤 
                newPos = new Vector2(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-15f, 15f));
                break;
        }

        // 3. 실제 위치 순간이동
        boss.transform.position = newPos;

        // 4. 다시 서서히 나타나기 (Fade In)
        if (sr != null)
        {
            sr.DOFade(1f, fadeInTime).SetId(boss.gameObject);
        }

        yield return new WaitForGameTime(fadeInTime);

        // 무적 해제
        if (col != null) col.enabled = true;

        yield return new WaitForGameTime(nextPatternDelay);
    }
}