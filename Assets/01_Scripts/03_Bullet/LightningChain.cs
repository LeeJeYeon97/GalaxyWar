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
    private bool _isMaxLevel; // 클래스 상단에 변수 추가

    // 이동을 위한 상태 변수들
    private GameObject _currentTarget;
    private bool _isMoving = false;

    // 1. 멤버 변수로 바구니 준비
    private Collider2D[] _chainColliders = new Collider2D[10];
    private ContactFilter2D _chainFilter;

    private void Awake()
    {
        //  최적화 2: 필터 초기화
        _chainFilter = new ContactFilter2D();
        _chainFilter.useLayerMask = true;
        _chainFilter.layerMask = LayerMask.GetMask("Meteor","Boss");
        _chainFilter.useTriggers = true;
    }
    public void Init(Vector3 startPos, GameObject firstTarget, float damage, float range, int count,bool maxLevel = false)
    {
        transform.position = startPos;
        _damage = damage;
        _range = range;
        _remainCount = count;
        _isMaxLevel = maxLevel;


        _visitedTargets.Clear();
        // 첫 번째 맞은 놈은 방금 총알한테 맞았으니(혹은 여기서 중복으로 안 때리기 위해) 제외 목록에 추가
        if (firstTarget != null)
        {
            _visitedTargets.Add(firstTarget);
        }

        if (_isMaxLevel && firstTarget != null && firstTarget.TryGetComponent(out MeteorController firstMeteor))
        {
            firstMeteor.Status.ApplyShock();
        }
        // 번개 전이 시작!
        StartCoroutine(CoChainProcess());
    }

    // FixedUpdate에서 이동 처리 (물리 엔진과 동기화!)
    private void FixedUpdate()
    {
        // 1. 아예 움직일 필요가 없으면 리턴
        if (!_isMoving) return;

        // 2. 이동 중에 타겟이 먼저 죽거나(오브젝트 풀로 돌아감) 사라진 경우!
        if (_currentTarget == null || !_currentTarget.activeSelf)
        {
            // 허공에 멈추지 않도록 상태를 초기화하고 코루틴 대기를 풀어줍니다.
            _currentTarget = null;
            _isMoving = false;
            return;
        }

        // 3. 물리 타임스텝에 맞춰 부드럽게 이동
        transform.position = Vector3.MoveTowards(transform.position, _currentTarget.transform.position, _speed * Time.fixedDeltaTime);

        // 4. 도착 판정
        if ((transform.position - _currentTarget.transform.position).sqrMagnitude < 0.01f)
        {
            OnTargetReached();
        }
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
            //  최적화 3: NonAlloc 방식 사용
            int hitCount = Physics2D.OverlapCircle(transform.position, _range, _chainFilter, _chainColliders);

            GameObject nextTarget = null;
            float minSqrDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                var col = _chainColliders[i];
                if (_visitedTargets.Contains(col.gameObject)) continue;

                float sqrDist = (transform.position - col.transform.position).sqrMagnitude;
                if (sqrDist < minSqrDistance)
                {
                    minSqrDistance = sqrDist;
                    nextTarget = col.gameObject;
                }
            }

            if (nextTarget == null) break;

            // 2. 타겟 설정 후 이동 시작
            _currentTarget = nextTarget;
            _visitedTargets.Add(_currentTarget);
            _remainCount--;
            _isMoving = true; // FixedUpdate가 이동을 시작하게 함

            //  3. 이동이 끝날 때까지 코루틴은 여기서 대기 (이동 중엔 while이 멈춤)
            while (_isMoving) yield return null;
        }

        yield return new WaitForSeconds(0.2f);
        Managers.Resource.Destroy(gameObject);
    }

    private void OnTargetReached()
    {
        // 데미지 처리
        if (_currentTarget.TryGetComponent(out MeteorController meteor))
        {
            meteor.OnDamage(_damage);
            if (_isMaxLevel) meteor.Status.ApplyShock();
            Managers.Sound.Play(Define.SoundID.Sfx_Lightning_Hit);

            if (Managers.Effect.CanSpawnEffect(meteor.transform.position))
            {
                GameObject hitGo = Managers.Resource.Instantiate(hitEffect);
                hitGo.transform.position = meteor.transform.position;
            }
        }

        // 이동 종료 및 다음 단계로
        _isMoving = false;
        _currentTarget = null;
    }
}
