using UnityEngine;
using static Define;

public class WaitForGameTime : CustomYieldInstruction
{
    private float _waitTime;
    private float _elapsedTime;

    // 생성자: 몇 초를 기다릴지 받습니다.
    public WaitForGameTime(float time)
    {
        _waitTime = time;
        _elapsedTime = 0f;
    }

    // 매 프레임마다 유니티 엔진이 이 속성을 검사합니다.
    // true면 계속 대기, false면 코루틴 다음 줄 실행!
    public override bool keepWaiting
    {
        get
        {
            // 핵심 방어 로직: 게임이 플레이 중일 때만 시간을 흐르게 합니다.
            if (Managers.Game.currentGameState == GameState.Playing)
            {
                _elapsedTime += Time.deltaTime;
            }

            // 아직 목표 시간에 도달하지 않았다면 계속 기다려라(true)
            return _elapsedTime < _waitTime;
        }
    }
}