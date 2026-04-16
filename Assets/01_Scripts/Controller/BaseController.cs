using UnityEngine;
using static Define;

public class BaseController : MonoBehaviour
{
    protected virtual void Update()
    {
        // 광고 중이거나 일시정지 상태라면 로직 실행 차단
        if (Managers.Game.currentGameState == GameState.Pause
            || Managers.Game.currentGameState == GameState.GameOver)
            return;

        OnUpdate();
    }

    protected virtual void FixedUpdate()
    {
        // 물리 연산도 동일하게 차단
        if (Managers.Game.currentGameState == GameState.Pause || Managers.Game.currentGameState == GameState.GameOver)
            return;

        OnFixedUpdate();
    }

    // 자식들은 이 함수들을 오버라이드해서 사용합니다.
    protected virtual void OnUpdate() { }
    protected virtual void OnFixedUpdate() { }
}