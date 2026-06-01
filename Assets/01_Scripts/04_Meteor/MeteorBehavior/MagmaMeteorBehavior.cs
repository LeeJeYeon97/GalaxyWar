using System.Collections;
using UnityEngine;
using static Define;

public class MagmaMeteorBehavior : IMeteorBehavior
{
    public void OnInit(MeteorController meteor)
    {
        // 1. 혹시라도 예전에 돌던 코루틴이 남아있다면 끕니다.
        //if (meteor.ActionCoroutine != null)
        //{
        //    meteor.StopCoroutine(meteor.ActionCoroutine);
        //}

        //// 2. 핵심! 매니저가 아닌 'meteor' 본체에게 코루틴 실행을 맡깁니다.
        //meteor.ActionCoroutine = meteor.StartCoroutine(CoDropMagma(meteor));
    }

    public void OnRelease(MeteorController meteor)
    {
        // 풀에 반환되거나 비활성화될 때 코루틴을 안전하게 정지합니다.
        if (meteor.ActionCoroutine != null)
        {
            meteor.StopCoroutine(meteor.ActionCoroutine);
            meteor.ActionCoroutine = null;
        }
    }

    public void OnUpdate(MeteorController meteor) { }
    public void OnDie(MeteorController meteor) 
    {
        GameObject magmaPrefab = meteor.Stat.magmaPuddle;
        if (magmaPrefab == null) return;

        float puddleDamage = meteor.Stat.Damage.TotalValue * 0.5f;
        Vector2 deathPos = meteor.transform.position;

        // 십자가 모양을 만들기 위한 4방향 (상, 하, 좌, 우)
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        // 브레스(장판)의 길이와 간격 설정 (기획에 맞게 수치를 조절하세요!)
        int breathLength = 1; // 한 방향당 깔리는 장판 개수 (3이면 총 1+12=13개 생성)
        float spacing = 1.0f; // 장판 사이의 간격

        // 1. 메테오가 죽은 정중앙 자리에 하나 생성
        SpawnPuddle(magmaPrefab, deathPos, puddleDamage);

        // 2. 4방향으로 뻗어나가며 장판 생성
        foreach (Vector2 dir in directions)
        {
            for (int i = 1; i <= breathLength; i++)
            {
                // 방향 * 간격 * 순번으로 위치를 계산합니다.
                Vector2 spawnPos = deathPos + (dir * spacing * i);
                SpawnPuddle(magmaPrefab, spawnPos, puddleDamage);
            }
        }
    }
    // 장판 생성을 깔끔하게 처리하기 위한 헬퍼 함수
    private void SpawnPuddle(GameObject prefab, Vector2 pos, float damage)
    {
        GameObject go = Managers.Resource.Instantiate(prefab);
        if (go != null)
        {
            go.transform.position = pos;
            MagmaPuddle puddle = go.GetComponent<MagmaPuddle>();
            if (puddle != null)
            {
                puddle.Init(pos, damage);
            }
        }
    }

    //private IEnumerator CoDropMagma(MeteorController meteor)
    //{
    //    //  1. 생명주기 안전장치: 운석이 씬에 살아있을 때만 무한 반복합니다.
    //    while (meteor != null && meteor.gameObject.activeInHierarchy)
    //    {
    //        //  2. 마법의 타이머: 게임이 일시정지되면 0.5초 타이머도 알아서 멈춥니다.
    //        yield return new WaitForGameTime(0.5f);

    //        //  3. 이중 방어막: 타이머가 끝난 찰나에 팝업이 떠서 멈췄다면 장판 생성을 스킵!
    //        if (Managers.Game.currentGameState == GameState.Pause)
    //        {
    //            continue;
    //        }

    //        // 안전 검사 통과 시 마그마 소환!
    //        GameObject magma = meteor.Stat.magmaPuddle;
    //        GameObject go = Managers.Resource.Instantiate(magma);
    //        if (go != null)
    //        {
    //            MagmaPuddle puddle = go.GetComponent<MagmaPuddle>();
    //            if (puddle != null)
    //            {
    //                float puddleDamage = meteor.Stat.Damage.TotalValue * 0.5f;
    //                puddle.Init(meteor.transform.position, puddleDamage);
    //            }
    //        }
    //    }
    //}
    //  시간차로 십자 장판을 퍼뜨리는 코루틴
    private IEnumerator CoSpreadMagmaBreath(GameObject prefab, Vector2 centerPos, float damage)
    {
        int breathLength = 2;  // 한 방향당 깔리는 장판 개수
        float spacing = 1.0f;  // 장판 사이의 간격
        float delay = 0.06f;   // 다음 장판이 터질 때까지의 지연 시간 (초 단위)

        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        // 1. 정중앙 자리에 첫 장판 생성
        SpawnPuddle(prefab, centerPos, damage);

        // 2. 중앙이 터진 후 첫 번째 지연 대기
        yield return new WaitForGameTime(delay);

        // 3. 1단계 거리부터 순차적으로 외곽으로 뻗어나감
        for (int i = 1; i <= breathLength; i++)
        {
            // 4방향을 동시에 생성하여 십자가가 사방으로 퍼지는 느낌을 줍니다.
            foreach (Vector2 dir in directions)
            {
                Vector2 spawnPos = centerPos + (dir * spacing * i);
                SpawnPuddle(prefab, spawnPos, damage);
            }

            // 다음 마디(마디 i+1)로 넘어가기 전에 잠깐 대기 (촤라락 연출의 핵심)
            yield return new WaitForGameTime(delay);
        }
    }

}
