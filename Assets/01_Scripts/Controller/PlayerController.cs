using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerController : MonoBehaviour
{

    [Header("Components")]
    private Rigidbody2D _rb;
    private LineRenderer lr;
    private Camera mainCam;

    [Header("State")]
    private PlayerState currentState = PlayerState.Idle;

    private bool _isReloading = false;
    public bool _isBurst = false;
    public bool _isInvincible = false;
    

    [Header("Stat")]    
    public float maxLineLength = 7f;    // 조준선 길이
    private float _lastShotTime;
    public float currentHp;
    public float currentDefence;
    public float currentBurst;
    public PlayerStat stat;

    [Header("Bullet")]
    public Transform _bulletPos;        // 총알이 나갈 발사구 위치
    public List<BulletController> bullets = new List<BulletController>();
    private Vector2 _currentAimDir;
    
    private Vector2 dragStartPos;
    private Vector2 dragPos;
    private Vector2 dragDir;

    private Coroutine _reloadCoroutine; // 코루틴 제어를 위한 변수
    
    // 이벤트 발생용
    private PlayerStatusEvent _curStatus = new PlayerStatusEvent();
    private Volume _volume;
    private ChromaticAberration _chromatic;
    public void Init()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;  // 우주니까 중력은 0


        mainCam = Camera.main;
        lr = GetComponent<LineRenderer>();
        lr.enabled = false;

        // 스탯 데이터 세팅
        stat = new PlayerStat();
        stat.SetStat(Managers.Data.playerStatData);
        currentHp = stat.maxHp.TotalValue;
        currentDefence = stat.maxDefence.TotalValue;
        currentBurst = 0f;


        // HUD업데이트 이벤트 발생
        OnStatusEvent();
   
        // 게임 시작 시 첫 장전
        Reload();

        // 씬에 있는 Global Volume을 찾아 효과 가져오기
        _volume = GameObject.FindFirstObjectByType<Volume>();
        if (_volume.profile.TryGet<ChromaticAberration>(out var ca))
        {
            _chromatic = ca;
        }

    }
    private void OnEnable()
    {
        Managers.Input.OnDragStarted += OnDragStart;
        Managers.Input.OnDragging += OnDragUpdate;
        Managers.Input.OnDragEnded += OnDragRelease;

        Managers.Event.Subscribe(ActionEvent.EnableBurstMode, OnEnableBurstMode);
    }

    private void OnDisable()
    {
        if (Managers.Input != null)
        {
            Managers.Input.OnDragStarted -= OnDragStart;
            Managers.Input.OnDragging -= OnDragUpdate;
            Managers.Input.OnDragEnded -= OnDragRelease;
        }
        Managers.Event.UnSubscribe(ActionEvent.EnableBurstMode, OnEnableBurstMode);
    }
    void Update()
    {
        
        if (currentState != PlayerState.Playing) return;
        
        // 발사 로직
        Shoot();
        AddBurstGauge();
    }
    private void FixedUpdate()
    {
        if (currentState != PlayerState.Playing) return;
        
        Move();
        Rotate();
    }

    public void SetState(PlayerState state)
    {
        currentState = state;
    }

    #region DrawLine
    void DrawReflectionLine(Vector2 startPos, Vector2 dir)
    {
        if (currentState != PlayerState.Playing) return;

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

    #region 이동관련
    void OnDragStart(Vector2 pos)
    {
        if (Managers.Game.currentGameState != GameState.Playing) return;
        dragStartPos = pos;
        lr.enabled = true;
    }
    void OnDragUpdate(Vector2 pos)
    {
        if (Managers.Game.currentGameState != GameState.Playing) return;

        dragPos = pos;
        // 터치한 시작 지점에서 현재 드래그하는 지점까지의 방향
        // (만약 반대 방향으로 움직이고 싶다면 순서를 바꾸세요)
        dragDir = (dragPos - dragStartPos).normalized;

        DrawReflectionLine(transform.position, dragDir);
    }

    void OnDragRelease()
    {
        lr.enabled = false; // 드래그 떼면 조준선 끄기
    }
    private void Move()
    {
        // 이동 입력이 없을 때는 리턴
        if (dragDir == Vector2.zero) 
            return;

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
        // 리로딩 중이면 리턴
        if (_isReloading) return;
        
        // 탄창이 비었으면 자동 리로드
        if (bullets.Count <= 0)
        {
            if (_reloadCoroutine == null)
                _reloadCoroutine = StartCoroutine(CoReload());
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
                Managers.Sound.Play(SoundID.Sfx_PlayerShot, Sound.Sfx);
                bullet.Shot(transform.up);          // 발사
                // 파티클
                //GameObject flash = Managers.Pool.Get<GameObject>(Define.Pool.NormalBullet_Flash);
                //if(flash != null)
                //{
                //    flash.transform.position = _bulletPos.position;
                //}

                // 발사 시간 설정
                _lastShotTime = Time.time;
            }
        }
    }

    // 리로드
    IEnumerator CoReload()
    {
        _isReloading = true;
        
        lr.enabled = false;

        Debug.Log("재장전 시작...");

        Managers.Event.PostEvent<float>(ActionEvent.ReloadStart, stat.reloadTime.TotalValue);
        Managers.Sound.Play(Define.SoundID.Sfx_Reloading);

        yield return new WaitForSeconds(stat.reloadTime.TotalValue);
        Reload();

        // 리로딩 끝
        Managers.Event.PostEvent(ActionEvent.ReloadEnd);
        _reloadCoroutine = null; // 완료 후 비워줌
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

            BulletStat stat;
            BulletController bullet;

            if (_isBurst)
            {
                // 버스트모드면 버스트 총알만 충전하기
                stat = Managers.Stat.GetBulletStat(Define.BulletType.BurstBullet);
            }
            else
            {
                // 버스트모드 아니면 랜덤으로 뽑기
                stat = Managers.Stat.GetRandomBulletStat();
            }

            if (stat == null) return;

            bullet = Managers.Pool.Get<BulletController>(stat.poolType);

            if (bullet == null) return;

            bullet.SetBullet(stat);
            bullets.Add(bullet);

        }
        _isReloading = false;

        Debug.Log("재장전 완료!");
    }
    #endregion

    #region 피격 및 사망
    public void OnDamage(float damage)
    {
        // 피격 무적
        if (_isInvincible) return;
        // 버스트모드면 무적 상태
        if (_isBurst) return;

        if (currentState != PlayerState.Playing) return;

        // 방어막이 있으면
        if (currentDefence > 0)
        {
            currentDefence -= damage;
        }
        // 방어막 없으면
        else
        {
            currentHp -= damage;
        }

        // hud업데이트 이벤트 발생
        OnStatusEvent();

        PlayGlitch();
        // 피격후에 짧은 무적시간
        StartCoroutine(CoInvincible());
        Managers.Sound.Play(SoundID.Sfx_PlayerHit);
        if (currentHp <= 0)
        {
            Debug.Log("죽었습니다.");
            // 죽는 처리
            Die();
        }
    }
    private void Die()
    {
        // 죽음 처리
        Managers.Game.ChangeGameState(Define.GameState.GameOver);
    }

    public void PlayGlitch()
    {
        // 0.2초 동안 강도를 올렸다가 내리는 연출 (DOTween 활용)
        DOTween.To(() => _chromatic.intensity.value,
                   x => _chromatic.intensity.value = x,
                   1f, 0.1f).SetLoops(2, LoopType.Yoyo);
    }

    IEnumerator CoInvincible()
    {
        _isInvincible = true;

        // 무적 시간 동안 스프라이트 깜빡이기 (예: 1초 동안 0.1초 간격)
        float elapsed = 0;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // 카메라가 0.2초 동안 0.5의 강도로 흔들림
        mainCam.transform.DOShakePosition(0.2f, 0.5f, 20, 90f).SetUpdate(true);

        while (elapsed < stat.hitCooldown)
        {
            // 투명도를 0.5와 1.0 사이로 교체
            sr.color = new Color(1, 1, 1, sr.color.a == 1f ? 0.5f : 1f);

            // 아주 짧은 대기도 캐싱해서 쓰면 더 좋습니다!
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        sr.color = Color.white; // 원래대로 복구
        _isInvincible = false;
    }

    #endregion

    #region 버스트 모드 관련
    private void OnEnableBurstMode()
    {
        stat.enableBurst = true;
    }

    // 게이지 증가 함수, 아이템 같은거로 회복시키면 isAuto를 false로 두고 amount로 값 넘겨주기
    public void AddBurstGauge(float amount = 0f, bool isAuto = true)
    {
        // 버스트 모드고 버스트모드 활성화 안되어있고 게이지가 꽉 안채워져있으면 채우기
        if (_isBurst == true 
            && stat.enableBurst == false
            && currentBurst >= stat.maxBurstGuage.TotalValue)
        {
            return;
        }

        float recoveryAmount = amount;
        // 자동으로 채울 때
        if (isAuto == true)
        {
            recoveryAmount = (stat.maxBurstGuage.TotalValue / stat.maxBurstFullChargeTime.TotalValue) * Time.deltaTime;
        }
        currentBurst = Mathf.Clamp(currentBurst + amount, 0, stat.maxBurstGuage.TotalValue);
        // hud업데이트 이벤트 발생
        OnStatusEvent();
    }

    
    // 버스트 모드 발동
    public void ActivateBurst()
    {
        if (currentState != PlayerState.Playing) return;
        // 이미 버스트 모드거나 버스트 능력 활성화 못했으면
        if (_isBurst || stat.enableBurst == false) return;

        if (currentBurst >= stat.maxBurstGuage.TotalValue)
        {
            _isBurst = true;
            Debug.Log("BURST MODE ACTIVATED!");

            StartCoroutine(BurstRoutine());
        }
        else
        {
            Debug.Log("BURST MODE 게이지 부족");
        }
    }
    private IEnumerator BurstRoutine()
    {
        Debug.Log("버스트 모드 시작");

        mainCam.DOOrthoSize(12.0f, 0.3f).SetEase(Ease.OutCubic).SetUpdate(true).OnUpdate(() => Managers.Map.UpdateMap());

        // 1. 스탯 강화
        stat.speed.AddMultiplier(1.0f);             // 이속 2배 증가
        stat.reloadTime.SetForceZero(true);         // 재장전 시간 0초로 고정
        stat.shotTime.AddMultiplier(-0.5f);         // 발사 속도 2배 감소

        // ★ 핵심: 진행 중인 리로드가 있다면 강제로 멈춤
        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }
        _isReloading = true;
        // 현재 장전(bullets 리스트)되어 있는 탄들을 모두 버스트 탄으로 변경
        Reload();

        // TODO : 여기서 연출이나 사운드 실행하기

        // 버스트 게이지 감소
        while (currentBurst > 0)
        {
            // 초당 10씩 감소 총 10초유지
            currentBurst -= 10.0f * Time.deltaTime;
            OnStatusEvent(); 
            yield return null;
        }

        // 3. 스탯 복구
        stat.speed.SubMultiplier(1.0f);
        stat.shotTime.SubMultiplier(-0.5f);
        stat.reloadTime.SetForceZero(false);

        currentBurst = 0;
        _isBurst = false;

        // 2. 카메라 시야 복구 (12.0 -> 9.6)
        mainCam.DOOrthoSize(9.6f, 0.3f).SetEase(Ease.OutCubic).SetUpdate(true).OnUpdate(() => Managers.Map.UpdateMap());

        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }
        _isReloading = true;
        // 현재 장전(bullets 리스트)되어 있는 탄들을 모두 버스트 탄으로 변경
        Reload();

        Debug.Log("버스트 모드 해체");
    }
    #endregion
    
    private void OnStatusEvent()
    {
        _curStatus.hp = currentHp;
        _curStatus.maxHp = stat.maxHp.TotalValue;
        _curStatus.shield = currentDefence;
        _curStatus.maxShield = stat.maxDefence.TotalValue;
        _curStatus.burst = currentBurst;
        _curStatus.maxBurst = stat.maxBurstGuage.TotalValue;
        // 3. 갱신된 방 통째로 신호를 보냅니다.
        Managers.Event.PostEvent<PlayerStatusEvent>(Define.ActionEvent.PlayerStatusChanged, _curStatus);
    }
    public void Revive()
    {
        if(currentState != PlayerState.Die)
        {
            return;
        }
        currentHp = stat.maxHp.TotalValue;
    }
}