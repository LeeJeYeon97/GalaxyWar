using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.UIElements;
using static Define;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviour
{

    [Header("Components")]
    private Rigidbody2D _rb;
    private LineRenderer lr;
    private Camera mainCam;

    [Header("State")]
    private bool _isReloading = false;
    private bool _isMoving;

    [Header("Stat")]    
    public float maxLineLength = 7f;    // 조준선 길이
    public PlayerStat stat;

    [Header("Bullet")]
    public Transform _bulletPos;        // 총알이 나갈 발사구 위치
    public List<BulletController> bullets = new List<BulletController>();
    private Vector2 _currentAimDir;
    
    private float _lastShotTime;

    private Vector2 dragStartPos;
    private Vector2 dragPos;
    private Vector2 dragDir;


    public void Init()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;  // 우주니까 중력은 0
        //_rb.linearDamping = 2.5f;

        mainCam = Camera.main;
        lr = GetComponent<LineRenderer>();
        lr.enabled = false;


        // 스탯 데이터 세팅
        stat = new PlayerStat();
        stat.SetStat(Managers.Data.playerStatData);

        if (Managers.Game.currentGameState != GameState.Playing) return;
        // 게임 시작 시 첫 장전
        Reload();
    }
    public void OnEnable()
    {
        Managers.Input.OnDragStarted += OnDragStart;
        Managers.Input.OnDragging += OnDragUpdate;
        Managers.Input.OnDragEnded += OnDragRelease;
    }

    public void OnDisable()
    {
        if (Managers.Input != null)
        {
            Managers.Input.OnDragStarted -= OnDragStart;
            Managers.Input.OnDragging -= OnDragUpdate;
            Managers.Input.OnDragEnded -= OnDragRelease;
        }
    }
    void Update()
    {
        if (Managers.Game.currentGameState != GameState.Playing) return;
        // 적 탐색
        //FindTarget();
        Shoot();
    }
    private void FixedUpdate()
    {
        if (Managers.Game.currentGameState != GameState.Playing) return;
        Move();
        Rotate();
    }

    #region DrawLine
    void DrawReflectionLine(Vector2 startPos, Vector2 dir)
    {
        lr.positionCount = 1;
        lr.SetPosition(0, startPos);

        float remainingDistance = maxLineLength;
        RaycastHit2D hit = Physics2D.Raycast(startPos, dir, remainingDistance, LayerMask.GetMask("Wall"));

        if (hit.collider != null)
        {
            lr.positionCount = 2;
            lr.SetPosition(1, hit.point);
        }
        else
        {
            lr.positionCount = 2;
            lr.SetPosition(1, startPos + dir * remainingDistance);
        }
    }
    #endregion

    #region Move/Rotate
    void OnDragStart(Vector2 pos)
    {
        if (Managers.Game.currentGameState != GameState.Playing) return;
        dragStartPos = pos;
        _isMoving = true;
        lr.enabled = true;
    }
    void OnDragUpdate(Vector2 pos)
    {
        if (Managers.Game.currentGameState != GameState.Playing) return;

        dragPos = pos;
        // 터치한 시작 지점에서 현재 드래그하는 지점까지의 방향
        // (만약 반대 방향으로 움직이고 싶다면 순서를 바꾸세요)
        dragDir = (dragPos - dragStartPos).normalized;

        // 조준선(LineRenderer) 업데이트: 움직이는 방향으로 선을 그려줌
        if (_isMoving)
        {
            lr.enabled = true;
            DrawReflectionLine(transform.position, dragDir);
        }
    }

    void OnDragRelease()
    {
        _isMoving = false;
        lr.enabled = false; // 드래그 떼면 조준선 끄기
        //dragDir = Vector2.zero;
    }
    private void Move()
    {
        // 이동 입력이 없을 때는 리턴
        if (!_isMoving || dragDir == Vector2.zero) return;

        // 2. 가속 힘 계산
        // moveForce를 고정값으로 두면 속도가 빠를수록 최고 속도 도달이 답답하게 느껴집니다.
        // 그래서 보통 maxSpeed의 일정 비율(예: 2~3배)을 힘으로 주면 체감이 좋습니다.
        float finalForce = stat.speed.TotalValue * 5f;

        // 1. 힘 가하기 (가속)
        _rb.AddForce(dragDir * finalForce, ForceMode2D.Force);

        // 2. 속도 제한
        if (_rb.linearVelocity.magnitude > stat.speed.TotalValue)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * stat.speed.TotalValue;
        }
    }
    private void Rotate()
    {
        // 3.우주선 회전(진행 방향 바라보기)
        // 부드러운 회전을 위해 리지드바디의 회전 기능을 사용합니다.
        float targetAngle = Mathf.Atan2(dragDir.y, dragDir.x) * Mathf.Rad2Deg;
        // -90f는 우주선의 스프라이트가 위(Y축)를 향하고 있을 때의 보정값입니다.
        _rb.MoveRotation(Mathf.LerpAngle(_rb.rotation, targetAngle - 90f, Time.fixedDeltaTime * 10f));
    }

    #endregion

    #region Weapon

    // 주변 적 탐색
    //public void FindTarget()
    //{
    //    // 타겟팅 타이머 (매 프레임 계산 방지)
    //    _targetTimer += Time.deltaTime;
    //    if (_targetTimer < _targetUpdateInterval)
    //    {
    //        return;
    //    }
    //    // 사거리에 따른 적 탐색
    //    // 리로딩 중이면 타겟 안 찾음
    //    if (_isReloading)
    //    {
    //        _target = null;
    //        return;
    //    }
    //    // Managers.Game에 있는 활성화된 메테오 리스트를 가져옵니다.
    //    if (Managers.Game.activeMeteors.Count == 0)
    //    {
    //        _target = null;
    //        return;
    //    }

    //    float minDistance = Mathf.Infinity; // 가장 짧은 거리를 저장할 변수
    //    foreach (var meteor in Managers.Game.activeMeteors)
    //    {
    //        if (meteor == null) continue;

    //        // 플레이어와 메테오 사이의 거리 계산
    //        float distance = Vector3.Distance(transform.position, meteor.transform.position);
            
    //        //탐지 범위 안에 있고, 지금까지 찾은 것보다 더 가깝다면 갱신
    //        if (distance <= stat.shotRange.TotalValue && distance < minDistance)
    //        {
    //            minDistance = distance;
    //            _target = meteor.gameObject;
    //        }

    //    }
    //    _targetTimer = 0;
    //}

    // 발사
    void Shoot()
    {
        if (_isReloading) return;
        

        // 탄창이 비었으면 자동 리로드
        if (bullets.Count <= 0 && !_isReloading)
        {
            StartCoroutine(CoReload());
            return;
        }


        // 3. 재장전 중이 아닐 때 연사 속도에 맞춰 사격
        if (Time.time - _lastShotTime >= stat.shotTime.TotalValue)
        {
            BulletController bullet = bullets[0];
            bullets.RemoveAt(0);

            if (bullet != null)
            {
                bullet.transform.position = _bulletPos.position;
                

                _currentAimDir = dragDir.normalized;
                bullet.Shot(transform.up);          // 발사


                // 파티클
                GameObject flash = Managers.Pool.Get<GameObject>(Define.Pool.NormalBullet_Flash);
                if(flash != null)
                {
                    flash.transform.position = _bulletPos.position;
                }

                // 발사 시간 설정
                _lastShotTime = Time.time;
            }
        }
    }

    // 리로드
    IEnumerator CoReload()
    {
        _isReloading = true;
        //_isTouching = false; // 리로드 중엔 사격 중단
        lr.enabled = false;

        Debug.Log("재장전 시작...");

        // 여기에 리로드 UI 게이즈 연출 추가 가능
        yield return new WaitForSeconds(stat.reloadTime.TotalValue);

        Reload();

        _isReloading = false;
        Debug.Log("재장전 완료!");
    }

    public void Reload()
    {
        // 남은 총알 청소
        foreach (var bullet in bullets)
        {
            if (bullet != null) 
                Managers.Pool.Release(bullet.gameObject);
        }
        bullets.Clear();

        // 탄창 가득 채우기
        int reloadCount = Mathf.FloorToInt(stat.reloadCount.TotalValue);
        for (int i = 0; i < reloadCount; i++)
        {

            BulletStat stat = Managers.Game.GetRandomBullet();
            BulletController bullet = Managers.Pool.Get<BulletController>(stat.poolType);

            if (bullet != null)
            {
                bullet.SetBullet(stat);
                bullets.Add(bullet);
            }
        }
    }
    #endregion

    public void OnDamage(float damage)
    {
        // 무적 상태
        // if (_isInvincible) return;

        stat.currentHp -= damage;

        if(stat.currentHp <= 0)
        {
            // 죽는 처리
            Die();
        }
        else
        {
            // 피격후에 짧은 무적시간
            //StartCoroutine(CoInvincible());
        }

    }
    private void Die()
    {
        // 죽음 처리
    }

    IEnumerator CoInvincible()
    {
        return null;
    }
}