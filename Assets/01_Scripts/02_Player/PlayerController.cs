using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerController : BaseController
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
    public PlayerStat Stat;

    public float _lastHomingShotTime;

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

    // 일시정지 대응을 위한 변수
    private Vector2 _savedVelocity;
    private float _savedAngularVelocity;
    private bool _physicsFrozenByPause = false;

    public float rotatePower;
    public float movePower;

    // 자동 조준(Auto Aim)을 위한 변수 추가
    private GameObject _target;
    private float _targetTimer = 0f;
    private float _targetUpdateInterval = 0.1f; // 0.1초마다 적을 찾음 (최적화)

    [Header("Weapon")]
    public Transform gunTransform;      // 유니티 인스펙터에서 미니건(포신) 오브젝트를 드래그해서 넣으세요!
    public float gunRotateSpeed = 20f;  // 미니건이 돌아가는 속도 (본체보다 빠르게 세팅하는 게 좋습니다)

    public void Init()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;  // 우주니까 중력은 0

        // [추진장치 느낌 내기] 선형 항력을 설정합니다.
        // 이 값이 클수록 손을 뗐을 때 더 빨리 멈춥니다. (기본값 2f~5f 추천)
        _rb.linearDamping = 0.5f;

        mainCam = Camera.main;
        lr = GetComponent<LineRenderer>();
        lr.enabled = false;

        // 스탯 데이터 세팅
        Stat = Managers.Stat.playerStat;
        currentHp = Stat.maxHp.TotalValue;
        currentDefence = Stat.maxDefence.TotalValue;
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
    //  FixedUpdate를 오버라이드하여 일시정지 시 물리를 완벽하게 멈춥니다.
    protected override void FixedUpdate()
    {
        
        if ((Managers.Game.currentGameState == GameState.Pause || Managers.Game.currentGameState == GameState.GameOver)
            && !_physicsFrozenByPause)
        {
            _savedVelocity = _rb.linearVelocity;
            _savedAngularVelocity = _rb.angularVelocity;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.simulated = false;
            _physicsFrozenByPause = true;
        }
        else if (!(Managers.Game.currentGameState == GameState.Pause && Managers.Game.currentGameState == GameState.GameOver) 
            && _physicsFrozenByPause)
        {
            _rb.simulated = true;
            _rb.linearVelocity = _savedVelocity;
            _rb.angularVelocity = _savedAngularVelocity;
            _physicsFrozenByPause = false;
        }

        base.FixedUpdate();
    }
    protected override void OnUpdate()
    {
        FindTarget();
        // 발사 로직
        Shoot();
        HomingShot();
        AddBurstGauge();
    }
    protected override void OnFixedUpdate()
    {
        //  게임 오버 등의 상태일 때는 아예 움직이지 못하게 막음
        if (currentState != PlayerState.Playing)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Move();
        Rotate();
        RotateGun();
    }

    public void SetState(PlayerState state)
    {
        currentState = state;
    }

    #region DrawLine
    //void DrawReflectionLine(Vector2 startPos, Vector2 dir)
    //{
    //    if (currentState != PlayerState.Playing) return;

    //    lr.positionCount = 1;
    //    lr.SetPosition(0, startPos);

    //    float remainingDistance = maxLineLength;
    //    RaycastHit2D hit = Physics2D.Raycast(startPos, dir, remainingDistance, LayerMask.GetMask("Wall"));

    //    if (hit.collider != null)
    //    {
    //        lr.positionCount = 2;
    //        lr.SetPosition(1, hit.point);
    //    }
    //    else
    //    {
    //        lr.positionCount = 2;
    //        lr.SetPosition(1, startPos + dir * remainingDistance);
    //    }
    //}
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

        //DrawReflectionLine(transform.position, dragDir);
    }

    void OnDragRelease()
    {
        //  드래그를 떼면 방향을 초기화하여 힘이 더 이상 들어가지 않게 합니다.
        dragDir = Vector2.zero;
        lr.enabled = false; // 드래그 떼면 조준선 끄기
    }
    private void Move()
    {
        //  입력이 없을 때
        if (dragDir == Vector2.zero)
        {
            // 선형 항력(linearDamping)이 설정되어 있으므로 가만히 두면 알아서 멈춥니다.
            return;
        }

        //  추진장치 로직: 
        // ForceMode2D.Force는 '지속적인 가속'을 줍니다.
        float finalForce = Stat.speed.TotalValue * movePower;
        _rb.AddForce(dragDir * finalForce, ForceMode2D.Force);

        // 속도 제한 (최고 속도 조절)
        if (_rb.linearVelocity.magnitude > Stat.speed.TotalValue)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * Stat.speed.TotalValue;
        }
    }
    private void Rotate()
    {
        Vector2 targetLookDir = Vector2.zero;

        // [회전 버그 수정] 우선순위를 정합니다.
        // 1. 드래그 중이라면 드래그 방향(조준 방향)을 봅니다.
        if (dragDir != Vector2.zero)
        {
            targetLookDir = dragDir;
        }
        // 2. 드래그 중이 아니어도 이동 속도가 남아있다면 미끄러지는 방향을 봅니다.
        else if (_rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            targetLookDir = _rb.linearVelocity.normalized;
        }

        // 아무런 입력도 없고 속도도 거의 없다면 회전하지 않고 마지막 각도를 유지합니다.
        if (targetLookDir == Vector2.zero) return;

        float targetAngle = Mathf.Atan2(targetLookDir.y, targetLookDir.x) * Mathf.Rad2Deg;
        float finalAngle = targetAngle - 90f;

        //  회전 속도를 5f -> 10f 정도로 높이면 더 기민하게 반응합니다.
        _rb.MoveRotation(Mathf.LerpAngle(_rb.rotation, finalAngle, Time.fixedDeltaTime * rotatePower));
    }

    #endregion

    #region Weapon

    // 주변 적 탐색
    public void FindTarget()
    {
        // 타겟팅 타이머 (매 프레임 계산 방지용 최적화)
        _targetTimer += Time.deltaTime;
        if (_targetTimer < _targetUpdateInterval)
        {
            return;
        }
        _targetTimer = 0f;

        // 리로딩 중이거나 게임 상태가 정상이 아니면 타겟 안 찾음
        if (currentState != PlayerState.Playing || Managers.Game.currentGameState != GameState.Playing)
        {
            _target = null;
            return;
        }

        float minDistance = Mathf.Infinity;
        _target = null;

        // 내 주변 지정된 사거리(shotRange) 안의 모든 콜라이더를 찾습니다.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, Stat.shotRange.TotalValue);

        foreach (Collider2D col in colliders)
        {
            MeteorController meteor = col.GetComponent<MeteorController>();

            // 콜라이더가 운석이고, 살아있는 상태라면
            if (meteor != null && meteor.gameObject.activeInHierarchy)
            {
                float distance = Vector2.Distance(transform.position, meteor.transform.position);

                // 지금까지 찾은 것보다 가깝다면 타겟 갱신
                if (distance < minDistance)
                {
                    minDistance = distance;
                    _target = meteor.gameObject;
                }
            }
        }
    }

    #region Gizmos
    // 플레이어 오브젝트를 클릭(Select)했을 때만 씬 뷰에 그려주는 함수입니다.
    // (항상 보이게 하려면 OnDrawGizmos() 로 이름을 바꾸시면 됩니다!)
    private void OnDrawGizmosSelected()
    {
        // 게임을 실행하기 전(에디터 상태)에는 Stat이 아직 할당되지 않아 
        // Null 에러가 날 수 있으므로 안전장치를 걸어줍니다.
        if (Stat == null || Stat.shotRange == null) return;

        // 눈에 잘 띄도록 반투명한 붉은색(또는 초록색)으로 색상을 설정합니다.
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

        // 내 위치를 중심으로 shotRange만큼의 크기를 가진 원(선)을 그립니다.
        Gizmos.DrawWireSphere(transform.position, Stat.shotRange.TotalValue);
    }
    #endregion

    // 발사
    void Shoot()
    {
        if (_isReloading) return;

        if (bullets.Count <= 0)
        {
            if (_reloadCoroutine == null)
                _reloadCoroutine = StartCoroutine(CoReload());
            return;
        }

        if (Time.time - _lastShotTime >= Stat.shotTime.TotalValue)
        {
            BulletController bullet = bullets[0];
            bullets.RemoveAt(0);

            if (bullet != null)
            {
                bullet.transform.position = _bulletPos.position;

                // 방향 결정: 타겟이 있으면 타겟 쪽으로, 없으면 내가 보는 앞쪽으로!
                Vector2 shootDir = transform.up;

                if (_target != null && _target.activeInHierarchy)
                {
                    shootDir = (_target.transform.position - _bulletPos.position).normalized;
                }

                _currentAimDir = shootDir;
                Managers.Sound.Play(SoundID.Sfx_PlayerShot, Sound.Sfx);

                if (Stat.isMultiShotEnabled && Stat.multiShotCount.TotalValue > 1)
                {
                    float multiShotChance = Random.Range(0f, 100f);
                    if (multiShotChance <= Stat.multiShotChance.TotalValue)
                    {
                        //멀티샷에도 새로 계산한 방향(shootDir)을 전달해 줍니다.
                        FireMultiShot(bullet, shootDir);
                    }
                    else
                    {
                        bullet.Shot(shootDir, _bulletPos.position);
                    }
                }
                else
                {
                    bullet.Shot(shootDir, _bulletPos.position);
                }
                _lastShotTime = Time.time;
            }
        }
    }

    // 유도탄 발사
    void HomingShot()
    {
        if(Stat.isHomingShotEnabled == false)
        {
            return;
        }

        if (Time.time - _lastHomingShotTime >= Stat.homingShotDelay.TotalValue)
        {
            BaseBulletStat homingStat = Managers.Stat.GetBulletStat(Define.BulletType.HomingBullet);
            GameObject go = Managers.Resource.Instantiate(homingStat.originalPrefabs);

            BulletController bullet = go.GetComponent<BulletController>();

            bullet.SetBullet(homingStat);
            bullet.Shot(transform.up, _bulletPos.position);

            // 발사 시간 설정
            _lastHomingShotTime = Time.time;
        }
    }

    // 새로 추가된 부채꼴 발사 핵심 로직!
    // 부채꼴 발사도 새로운 발사 방향(baseShootDir)을 기준으로 계산하도록 수정!
    private void FireMultiShot(BulletController mainBullet, Vector2 baseShootDir)
    {
        // 기준 각도를 플레이어 앞면(transform.up)이 아닌 전달받은 방향(baseShootDir)으로 바꿉니다.
        float baseAngle = Mathf.Atan2(baseShootDir.y, baseShootDir.x) * Mathf.Rad2Deg;

        float startAngle = baseAngle - (Stat.multiShotAngle / 2f);
        float angleStep = Stat.multiShotAngle / (Stat.multiShotCount.TotalValue - 1);

        for (int i = 0; i < Stat.multiShotCount.TotalValue; i++)
        {
            BulletController bulletToFire;

            if (i == 0)
            {
                bulletToFire = mainBullet;
            }
            else
            {
                GameObject go = Managers.Resource.Instantiate(mainBullet.Stat.originalPrefabs);
                bulletToFire = go?.GetComponent<BulletController>();

                if (bulletToFire != null)
                {
                    bulletToFire.SetBullet(Managers.Stat.GetBulletStat(mainBullet.Stat.type));
                }
            }

            if (bulletToFire != null)
            {
                float currentAngle = startAngle + (angleStep * i);
                Vector2 shotDir = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad)).normalized;

                bulletToFire.transform.position = _bulletPos.position;
                bulletToFire.Shot(shotDir, _bulletPos.position);
            }
        }
    }

    // 리로드
    IEnumerator CoReload()
    {
        _isReloading = true;
        
        lr.enabled = false;

        Managers.Event.PostEvent<float>(ActionEvent.ReloadStart, Stat.reloadTime.TotalValue);
        Managers.Sound.Play(Define.SoundID.Sfx_Reloading);

        yield return new WaitForGameTime(Stat.reloadTime.TotalValue);

        Reload();

        // 리로딩 끝
        Managers.Event.PostEvent(ActionEvent.ReloadEnd);
        _reloadCoroutine = null; // 완료 후 비워줌
    }

    private void RotateGun()
    {
        // 미니건이 할당되어 있지 않으면 에러 방지
        if (gunTransform == null) return;

        // 1. 기본적으로는 비행기의 정면(앞쪽)을 바라봅니다.
        Vector2 aimDir = transform.up;

        // 2. 만약 찾아둔 타겟이 있다면, 미니건부터 타겟까지의 방향을 계산합니다.
        if (_target != null && _target.activeInHierarchy)
        {
            aimDir = (_target.transform.position - gunTransform.position).normalized;
        }

        // 3. 방향 벡터를 각도(Degree)로 변환합니다.
        float targetAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

        // 4. 스프라이트의 앞쪽이 위(Y)를 향한다면 -90을 빼줍니다.
        // (만약 총구 스프라이트가 오른쪽(X)을 보고 그려져 있다면 -90f를 지우세요!)
        float finalAngle = targetAngle - 90f;

        // 5. 미니건을 목표 각도를 향해 부드럽게(혹은 빠르게) 회전시킵니다.
        Quaternion targetRotation = Quaternion.Euler(0, 0, finalAngle);
        gunTransform.rotation = Quaternion.Lerp(gunTransform.rotation, targetRotation, Time.fixedDeltaTime * gunRotateSpeed);
    }

    public void Reload()
    {
        // 남은 총알 청소
        foreach (var bullet in bullets)
        {
            Managers.Resource.Destroy(bullet.gameObject);
        }
        bullets.Clear();

        // 탄창 가득 채우기
        int reloadCount = Mathf.FloorToInt(Stat.reloadCount.TotalValue);
        for (int i = 0; i < reloadCount; i++)
        {

            BaseBulletStat stat;
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

            GameObject go = Managers.Resource.Instantiate(stat.originalPrefabs);
            BulletController bullet = go?.GetComponent<BulletController>();

            if (bullet == null) return;

            bullet.SetBullet(stat);
            bullets.Add(bullet);

        }
        _isReloading = false;

    }
    #endregion

    #region 피격 및 사망
    public void OnDamage(float damage)
    {
        if(Managers.Data.GameData.playerGod)
        {
            return;
        }
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
        _isInvincible = true;
        float elapsed = 0;

        // 1. SpriteRenderer 대신 MeshRenderer를 가져옵니다.
        //MeshRenderer mr = GetComponent<MeshRenderer>();

        // (만약 플레이어 비주얼 오브젝트가 자식으로 있다면 아래처럼 가져오세요)
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();

        mainCam.transform.DOShakePosition(0.2f, 0.5f, 20, 90f).SetUpdate(true);

        // 2. PropertyBlock과 셰이더 색상 이름표 준비 (URP 기본은 "_BaseColor", 스탠다드는 "_Color")
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        string colorProp = "_BaseColorTint"; // 디버그 모드에서 찾으신 진짜 이름표로 바꿔주세요!

        bool isTransparent = false; // 깜빡임 상태를 체크할 스위치

        while (elapsed < Stat.hitCooldown)
        {
            if (mr != null)
            {
                mr.GetPropertyBlock(mpb);

                // 스위치 상태에 따라 알파(투명도) 값을 0.5 또는 1.0으로 결정
                float targetAlpha = isTransparent ? 1f : 0.5f;

                // 색상은 원래 색(보통 흰색)을 유지하고, 마지막 알파값만 바꿔줍니다.
                mpb.SetColor(colorProp, new Color(1f, 1f, 1f, targetAlpha));

                mr.SetPropertyBlock(mpb);

                // 다음 턴을 위해 스위치 뒤집기
                isTransparent = !isTransparent;
            }

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        // 3. 무적 종료 시 원래대로 (불투명한 원래 색상) 복구
        if (mr != null)
        {
            mr.GetPropertyBlock(mpb);
            mpb.SetColor(colorProp, Color.white);
            mr.SetPropertyBlock(mpb);
        }

        _isInvincible = false;
    }

    #endregion

    #region 버스트 모드 관련
    private void OnEnableBurstMode()
    {
        Stat.enableBurst = true;
    }

    // 게이지 증가 함수, 아이템 같은거로 회복시키면 isAuto를 false로 두고 amount로 값 넘겨주기
    public void AddBurstGauge(float amount = 0f, bool isAuto = true)
    {
        // 버스트 모드 활성화 안되어 있으면 리턴
        if(Stat.enableBurst == false)
        {
            return;
        }
        // 현재 버스트 모드면 리턴
        if(_isBurst == true)
        {
            return;
        }
        // 현재 버스트 게이지가 꽉채워져있으면 리턴
        if (currentBurst >= Stat.maxBurstGuage.TotalValue)
        {
            return;
        }

        float recoveryAmount = amount;
        // 자동으로 채울 때
        if (isAuto == true)
        {
            recoveryAmount = (Stat.maxBurstGuage.TotalValue / Stat.maxBurstFullChargeTime.TotalValue) * Time.deltaTime;
        }
        currentBurst = Mathf.Clamp(currentBurst + recoveryAmount, 0, Stat.maxBurstGuage.TotalValue);
        // hud업데이트 이벤트 발생
        OnStatusEvent();
    }

    
    // 버스트 모드 발동
    public void ActivateBurst()
    {
        if (currentState != PlayerState.Playing) return;
        // 이미 버스트 모드거나 버스트 능력 활성화 못했으면
        if (_isBurst || Stat.enableBurst == false) return;

        if (currentBurst >= Stat.maxBurstGuage.TotalValue)
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
        Stat.speed.AddMultiplier(1.0f);             // 이속 2배 증가
        Stat.reloadTime.SetForceZero(true);         // 재장전 시간 0초로 고정
        Stat.shotTime.AddMultiplier(-0.5f);         // 발사 속도 2배 감소

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
            
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                yield return null;
                continue;
            }
            currentBurst -= 10.0f * Time.deltaTime;
            OnStatusEvent();
            yield return null;
        }

        // 3. 스탯 복구
        Stat.speed.SubMultiplier(1.0f);
        Stat.shotTime.SubMultiplier(-0.5f);
        Stat.reloadTime.SetForceZero(false);

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
        _curStatus.maxHp = Stat.maxHp.TotalValue;
        _curStatus.shield = currentDefence;
        _curStatus.maxShield = Stat.maxDefence.TotalValue;
        _curStatus.burst = currentBurst;
        _curStatus.maxBurst = Stat.maxBurstGuage.TotalValue;
        // 3. 갱신된 방 통째로 신호를 보냅니다.
        Managers.Event.PostEvent<PlayerStatusEvent>(Define.ActionEvent.PlayerStatusChanged, _curStatus);
    }
    public void Revive()
    {
        if(currentState != PlayerState.Die)
        {
            return;
        }
        currentHp = Stat.maxHp.TotalValue;
        OnStatusEvent();
    }
    public void UpdateMaxHp(float value)
    {
        currentHp += value;
        if(currentHp >= Stat.maxHp.TotalValue)
        {
            currentHp = Stat.maxHp.TotalValue;
        }
    }

}