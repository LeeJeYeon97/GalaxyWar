using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using EnergyShield;

public class PlayerController : BaseController, IDamageable
{
    private Camera mainCam;

    [Header("Modules")]
    public PlayerMovement Movement { get; private set; } // 모듈 접근용 변수 추가
    public PlayerCombat Combat { get; private set; }
    public AttackRangeIndicator AttackRangeIndicator { get; private set; }
    public PlayerMagnetic Magnetic { get; private set; }


    [Header("State")]
    public PlayerState currentState = PlayerState.Idle; // 접근을 위해 public으로 변경
    public PlayerStat Stat; // 접근을 위해 public 유지

    public bool _isBurst = false;
    public bool _isInvincible = false;
    

    [Header("Stat")]    
    public float currentHp;
    public float currentDefence;
    public float currentDefenceGuage;
    public float shieldCoolTime;
    public float currentBurst;
    
    // 이벤트 발생용
    private PlayerStatusEvent _curStatus = new PlayerStatusEvent();
    private Volume _volume;
    private ChromaticAberration _chromatic;

    public SpriteRenderer sr;

    public GameObject shield;
    public float hitMaxStrength = 5f;
    public float hitRadius = 2f;
    public float hitLerpSpeed = 10f;
    public bool shieldhit = false;

    public UI_HpWarningPopup warningpopup;

    // 1. 클래스 상단에 변수 캐싱 (매번 생성하지 않기 위해)
    private MaterialPropertyBlock _mpb;
    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");

    public void Init()
    {
        //2. 게임 시작할 때 딱 한 번만 바구니를 만들어 둡니다!
        _mpb = new MaterialPropertyBlock();
        mainCam = Camera.main;
        shieldCoolTime = 0;
        // 스탯 데이터 세팅
        Stat = Managers.Stat.playerStat;
        currentHp = Stat.maxHp.TotalValue;
        currentDefence = Stat.maxDefenceCount.TotalValue;
        currentBurst = 0f;
        currentDefenceGuage = 0;
        shieldhit = false;
        Movement = GetComponent<PlayerMovement>();
        if (Movement != null) Movement.Init(this);

        Combat = GetComponent<PlayerCombat>();
        if (Combat != null) Combat.Init(this);

        Magnetic = GetComponentInChildren<PlayerMagnetic>();
        if (Magnetic != null) Magnetic.Init(this);

        // HUD업데이트 이벤트 발생
        OnStatusEvent();

        shield.gameObject.SetActive(false);
        // 씬에 있는 Global Volume을 찾아 효과 가져오기
        _volume = GameObject.FindFirstObjectByType<Volume>();
        if (_volume.profile.TryGet<ChromaticAberration>(out var ca))
        {
            _chromatic = ca;
        }

        AttackRangeIndicator = GetComponentInChildren<AttackRangeIndicator>();

        // 찾은 모듈 세팅 실행
        if (AttackRangeIndicator != null)
        {
            // 게임 플레이 모드일 때만 초기화 (에디터 모드에서는 OnValidate가 대신 그려줌)
            if (Application.isPlaying)
            {
                AttackRangeIndicator.SetupLineRenderer();
            }
        }

    }
    private void OnEnable()
    {
        Managers.Event.Subscribe(ActionEvent.EnableBurstMode, OnEnableBurstMode);
    }

    private void OnDisable()
    {
        Managers.Event.UnSubscribe(ActionEvent.EnableBurstMode, OnEnableBurstMode);
    }
    protected override void OnUpdate()
    {
        // [추가된 방어 코드] 게임이 일시정지 상태면 게이지 회복 중지
        if (Managers.Game.currentGameState != Define.GameState.Playing)
        {
            return;
        }
        AddBurstGauge();
        AddShieldGauge();
    }
    public void SetState(PlayerState state)
    {
        currentState = state;
    }

    public void AddShieldGauge()
    {
        // 1. 패시브를 안 배웠거나, 최대 방어막 횟수가 0 이하라면 작동 안 함 (무한 루프 버그 해결)
        if (Managers.Ability.GetCurrentLevel(Define.AbilityType.Passive_PlayerShield) <= 0 || Stat.maxDefenceCount.TotalValue <= 0)
        {
            return;
        }

        // 2. 이미 방어막이 꽉 차 있으면 작동 안 함
        if (currentDefence > 0)
        {
            return;
        }

        // 3. 쿨타임 증가
        shieldCoolTime += Time.deltaTime;

        // 핵심: 쿨타임 진행률(0.0 ~ 1.0)을 계산하여 100을 곱합니다.
        float progress = Mathf.Clamp01(shieldCoolTime / Stat.shieldChargeTime.TotalValue);
        currentDefenceGuage = progress * 100f;

        // 4. 충전 완료!
        if (shieldCoolTime >= Stat.shieldChargeTime.TotalValue)
        {
            // 방어막을 꽉 채워주고 쿨타임을 0으로 초기화합니다.
            currentDefence = Stat.maxDefenceCount.TotalValue;
            currentDefenceGuage = Stat.maxDefenceGuage;

            shield.gameObject.SetActive(true);
            shieldhit = false;
            shieldCoolTime = 0f;
        }

        // HUD 업데이트 (게이지가 차오르는 것을 보여주기 위함)
        OnStatusEvent();
    }
    public void UpgradeShield(float downTime, int shieldCount = 0)
    {
        // 1. 쿨타임 감소 적용
        Stat.shieldChargeTime.SubValue(downTime);

        // 2. 쉴드 최대 갯수 증가
        if (shieldCount > 0)
        {
            Stat.maxDefenceCount.AddValue(shieldCount);

            // 최대치가 늘어났을 때 현재 쉴드가 없다면 즉시 채워줍니다.
            if (currentDefence <= 0)
            {
                currentDefence = Stat.maxDefenceCount.TotalValue;
                currentDefenceGuage = Stat.maxDefenceGuage;
                shieldCoolTime = 0f; // 쿨타임 초기화
                shield.gameObject.SetActive(true);
                shieldhit = false;
                OnStatusEvent();
            }
        }
    }
    #region 피격 및 사망
    public void OnDamage(float damage, bool isCrit = false, GameObject attacker = null)
    {
#if UNITY_EDITOR
        if(Managers.Data.GameData.playerGod)
        {
            return;
        }
#endif
        // 피격 무적
        if (_isInvincible) return;
        // 버스트모드면 무적 상태
        if (_isBurst) return;

        if (currentState != PlayerState.Playing) return;

        
        // 방어막이 있으면
        if (currentDefence > 0)
        {
            currentDefence--;
            // Check if the object we hit has the shield pulse script
            ShieldHitPulse shieldPulse = shield.GetComponent<ShieldHitPulse>();
            if (shieldPulse != null)
            {
                shieldPulse.TriggerPulse(transform.position, hitMaxStrength, hitRadius, hitLerpSpeed);
            }
            shieldhit = true;
            // 방어막 히트 이벤트
            Managers.Effect.Play(EffectType.Screen_ShieldHit, Vector2.zero);
            Managers.Sound.Play(SoundID.Sfx_ShieldHit);
        }
        // 방어막 없으면
        else
        {
            // 공격자가 있을 경우 이름과 ID를 가져오고, 없으면 "알 수 없음"으로 처리
            string attackerName = attacker != null ? attacker.name : "알 수 없음";
            int attackerID = attacker != null ? attacker.GetInstanceID() : 0;

            // 시간, 데미지, 공격자 이름, 고유 ID를 한 번에 출력
            Debug.Log($"[피격] 데미지: {damage} | 맞은 시간: {Time.time:F3} | 때린 놈: {attackerName} | 인스턴스 ID: {attackerID}");
            currentHp -= damage;
            Managers.Sound.Play(SoundID.Sfx_PlayerHit);
            // 그냥 히트 이펙트
            //Managers.Effect.Play(EffectType.Screen_PlayerHit, Vector2.zero);
        }


        //  [추가/수정] 체력 30% 이하 경고 팝업 제어 로직
        // (최대 체력 변수명은 대표님 프로젝트의 세팅에 맞게 Stat.maxHp.TotalValue 또는 MaxHp 등으로 맞춰주세요!)
        float maxHp = Stat.maxHp.TotalValue;
        float hpRatio = currentHp / maxHp;

        if (hpRatio <= 0.3f && currentHp > 0)
        {
            // 체력이 30% 이하일 때: 팝업이 아직 생성되지 않았다면 띄우고 재생합니다.
            if (warningpopup == null)
            {
                warningpopup = Managers.UI.ShowPopupUI<UI_HpWarningPopup>();
            }

            // 만약 체력별로 속도를 다르게 하고 싶다면 여기서 분기 처리를 하셔도 좋습니다!
            // 예: 10% 이하면 더 빠르게 깜빡이도록 구현 가능
            warningpopup.PlayWarning();
        }
        else
        {
            // 체력이 30%를 초과했거나 죽었을 때: 켜져 있는 팝업이 있다면 정지시킵니다.
            if (warningpopup != null)
            {
                warningpopup.StopWarning();
                warningpopup = null; // 참조를 비워주어야 다음에 다시 체력이 떨어질 때 새로 생성됩니다.
            }
        }

        // hud업데이트 이벤트 발생
        OnStatusEvent();

        PlayGlitch();
        // 피격후에 짧은 무적시간
        StartCoroutine(CoInvincible());
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
        float elapsed = 0;

        // 카메라 쉐이크
        mainCam.transform.DOShakePosition(0.2f, 0.5f, 20, 90f).SetUpdate(true);

        bool isFlash = false;

        while (elapsed < Stat.hitCooldown)
        {
            if (sr != null)
            {
                //  4. 새로 만들지 않고, 아까 만들어둔 _mpb 바구니를 재사용합니다!
                sr.GetPropertyBlock(_mpb);

                // 플래시 상태면 엄청 밝은 흰색, 아니면 그냥 원래 기본 색상(흰색)
                Color targetColor = isFlash ? new Color(2f, 2f, 2f, 1f) : Color.white;

                // 문자열 "_FlashColor" 대신 미리 찾아둔 ID 사용
                _mpb.SetColor(FlashColorID, targetColor);
                sr.SetPropertyBlock(_mpb);

                isFlash = !isFlash;
            }

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        // 무적 종료 시 원상 복구
        if (sr != null)
        {
            sr.GetPropertyBlock(_mpb);
            _mpb.SetColor(FlashColorID, Color.white);
            sr.SetPropertyBlock(_mpb);
        }

        if(shieldhit)
            shield.gameObject.SetActive(false);

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

        // 4. 버스트 게이지 MAX 도달 시 1회성 이벤트 발동 (사운드 + UI 팝업)
        if (currentBurst >= Stat.maxBurstGuage.TotalValue)
        {
            // 도파민 터지는 알람 사운드!
            Managers.Sound.Play(SoundID.Sfx_BurstModeOnAlarm);

            // 우측에서 촥! 날아오는 배너 팝업 띄우기!
            Managers.UI.ShowPopupUI<UI_BurstOnPopup>();
        }
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
            Managers.Sound.Play(Define.SoundID.Sfx_BurstModeOn);

            StartCoroutine(BurstRoutine());
        }
    }
    private IEnumerator BurstRoutine()
    {
        mainCam.DOOrthoSize(Managers.Data.GameData.burstModeSize, 0.3f)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnUpdate(() =>
            {
                Managers.Map.UpdateMap();
                //Managers.Effect.Play(EffectType.Screen_BurstMode, Vector3.zero);
            });


        Stat.ApplyBurstBuff();
        //  핵심: Combat 모듈을 통해 강제 리로드 진행
        if (Combat != null)
        {
            Combat.CancelReload();
            Combat.isReloading = true;
            Combat.Reload();
        }

        while (currentBurst > 0)
        {
            if (Managers.Game.currentGameState == GameState.Pause || Managers.Game.currentGameState == GameState.GameOver) 
            {
                yield return null; 
                continue; 
            }
            currentBurst -= 10.0f * Time.deltaTime;
            OnStatusEvent();
            yield return null;
        }

        Stat.speed.SubMultiplier(1.0f);
        Stat.shotTime.SubMultiplier(-0.5f);
        Stat.reloadTime.SetForceZero(false);
        currentBurst = 0;
        _isBurst = false;

        float cameraSize = Managers.Data.GameData.gamePlayeSize;
        if (Managers.Game.isBossSpawn)
        {
            cameraSize = Managers.Data.GameData.bossStageSize;
        }

        mainCam.DOOrthoSize(cameraSize, 0.3f)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnUpdate(() => Managers.Map.UpdateMap());

        Stat.RemoveBurstBuff();

        // 버스트 종료 시 재장전 복구
        if (Combat != null)
        {
            Combat.CancelReload();
            Combat.isReloading = true;
            Combat.Reload();
        }
    }
    #endregion

    private void OnStatusEvent()
    {
        _curStatus.hp = currentHp;
        _curStatus.maxHp = Stat.maxHp.TotalValue;
        _curStatus.shieldCount = currentDefence;
        _curStatus.maxShieldGuage = Stat.maxDefenceGuage;
        _curStatus.currentShieldGuage = currentDefenceGuage;
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

        if (warningpopup != null)
        {
            warningpopup.StopWarning();
            warningpopup = null; // 참조를 비워주어야 다음에 다시 체력이 떨어질 때 새로 생성됩니다.
        }
        SetState(PlayerState.Playing);
        OnStatusEvent();
    }
    public void UpdateMaxHp(float value)
    {
        Stat.maxHp.AddValue(value);
        currentHp += value;

        if (currentHp >= Stat.maxHp.TotalValue)
        {
            currentHp = Stat.maxHp.TotalValue;
        }

        float maxHp = Stat.maxHp.TotalValue;
        float hpRatio = currentHp / maxHp;

        if (hpRatio <= 0.3f && currentHp > 0)
        {
            // 체력이 30% 이하일 때: 팝업이 아직 생성되지 않았다면 띄우고 재생합니다.
            if (warningpopup == null)
            {
                warningpopup = Managers.UI.ShowPopupUI<UI_HpWarningPopup>();
            }

            // 만약 체력별로 속도를 다르게 하고 싶다면 여기서 분기 처리를 하셔도 좋습니다!
            // 예: 10% 이하면 더 빠르게 깜빡이도록 구현 가능
            warningpopup.PlayWarning();
        }
        else
        {
            // 체력이 30%를 초과했거나 죽었을 때: 켜져 있는 팝업이 있다면 정지시킵니다.
            if (warningpopup != null)
            {
                warningpopup.StopWarning();
                warningpopup = null; // 참조를 비워주어야 다음에 다시 체력이 떨어질 때 새로 생성됩니다.
            }
        }

        OnStatusEvent();
    }
    public void HealCurrentHp(float value)
    {
        if (value <= 0) return;

        currentHp += value;
        if (currentHp >= Stat.maxHp.TotalValue)
        {
            currentHp = Stat.maxHp.TotalValue;
        }
        OnStatusEvent();
    }
}