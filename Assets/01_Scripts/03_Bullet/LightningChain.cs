using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class LightningChain : MonoBehaviour
{
    private float _damage;
    private float _range;
    private int _remainCount;
    public float _speed;

    public GameObject hitEffect;
    private HashSet<GameObject> _visitedTargets = new HashSet<GameObject>();
    private LayerMask _targetLayer;
    private bool _isMaxLevel; // 클래스 상단에 변수 추가

    public void Init(Vector3 startPos, GameObject firstTarget, float damage, float range, int count,bool maxLevel = false)
    {
        transform.position = startPos;
        _damage = damage;
        _range = range;
        _remainCount = count;
        _isMaxLevel = maxLevel;
        _targetLayer = LayerMask.GetMask("Meteor");

        _visitedTargets.Clear();
        // 첫 번째 맞은 놈은 방금 총알한테 맞았으니(혹은 여기서 중복으로 안 때리기 위해) 제외 목록에 추가
        if (firstTarget != null)
        {
            _visitedTargets.Add(firstTarget);
        }

        if (_isMaxLevel && firstTarget != null)
        {
            MeteorController firstMeteor = firstTarget.GetComponent<MeteorController>();
            if (firstMeteor != null)
            {
                firstMeteor.Status.ApplyShock();
            }
        }
        // 번개 전이 시작!
        StartCoroutine(CoChainProcess());
    }

    private IEnumerator CoChainProcess()
    {
        while (_remainCount > 0)
        {

            if (Managers.Game.currentGameState == GameState.Pause)
            {
                yield return null;
                continue;
            }

            // 1. 현재 내 위치를 기준으로 다음 타겟 찾기
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, _range, _targetLayer);
            Collider2D closestEnemy = null;
            float minDistance = float.MaxValue;

            foreach (var col in colliders)
            {
                if (_visitedTargets.Contains(col.gameObject)) continue;

                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestEnemy = col;
                }
            }

            // 2. 주변에 더 이상 타겟이 없으면 조기 퇴근!
            if (closestEnemy == null) break;

            // 3. 다음 타겟 확정
            GameObject targetGo = closestEnemy.gameObject;
            _visitedTargets.Add(targetGo);
            _remainCount--;

            // 4. 타겟을 향해 미친 듯이 날아감 (업데이트처럼 작동)
            // 만약 날아가는 도중에 적이 다른 공격에 맞아 죽으면(activeSelf == false) 자연스럽게 루프 탈출
            while (targetGo != null && targetGo.activeSelf)
            {
                // Vector3.MoveTowards를 쓰면 목표를 향해 아주 깔끔하게 직선 이동합니다.
                transform.position = Vector3.MoveTowards(transform.position, targetGo.transform.position, _speed * Time.deltaTime);

                // 적에게 거의 다다랐다면? (충돌 판정)
                if ((transform.position - targetGo.transform.position).sqrMagnitude < 0.01f)
                {
                    MeteorController meteor = targetGo.GetComponent<MeteorController>();
                    if (meteor != null)
                    {
                        // 찌릿! 데미지 주기
                        meteor.OnDamage(_damage);

                        // 데미지가 들어간 직후에 감전 주기
                        if (_isMaxLevel)
                        {
                            meteor.Status.ApplyShock();
                        }

                        Managers.Sound.Play(Define.SoundID.Sfx_Lightning_Hit);
                        GameObject hitGo = Managers.Resource.Instantiate(hitEffect);

                        if(hitGo != null)
                        {
                            hitGo.transform.position = meteor.transform.position;
                        }
                    }
                    break; // 데미지를 줬으니 다음 적을 찾으러 안쪽 while문 탈출
                }

                yield return null; // 1프레임 대기
            }
        }

        // 5. 전이가 다 끝났다면 씬에서 사라지기 (풀에 반납)
        // 만약 꼬리(Trail Renderer)가 달려있다면, 잔상이 사라질 수 있도록 0.5초 정도 대기 후 꺼주는 게 예쁩니다.
        yield return new WaitForSeconds(0.2f);
        Managers.Resource.Destroy(gameObject);
    }
}
