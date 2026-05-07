using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

public class PlayerController : BaseController
{
    private Camera mainCam;

    [Header("Modules")]
    public PlayerMovement Movement { get; private set; } // 모듈 접근용 변수 추가
    public PlayerCombat Combat { get; private set; }


    [Header("State")]
    public PlayerState currentState = PlayerState.Idle; // 접근을 위해 public으로 변경
    public PlayerStat Stat; // 접근을 위해 public 유지

    public bool _isBurst = false;
    public bool _isInvincible = false;
    

    [Header("Stat")]    
    public float currentHp;
    public float currentDefence;
    public float shieldCoolTime;
    public float currentBurst;
    
    // 이벤트 발생용
    private PlayerStatusEvent _curStatus = new PlayerStatusEvent();
    private Volume _volume;
    private ChromaticAberration _chromatic;

    public SpriteRenderer sr;

    public void Init()
    {

        mainCam = Camera.main;
        shieldCoolTime = 0;
        // 스탯 데이터 세팅
        Stat = Managers.Stat.playerStat;
        currentHp = Stat.maxHp.TotalValue;
        currentDefence = Stat.maxDefence.TotalValue;
        currentBurst = 0f;

        Movement = GetComponent<PlayerMovement>();
        if (Movement != null) Movement.Init(this);

        Combat = GetComponent<PlayerCombat>();
        if (Combat != null) Combat.Init(this);

        // HUD업데이트 이벤트 발생
        OnStatusEvent();
   
        // 씬에 있는 Global Volume을 찾아 효과 가져오기
        _volume = GameObject.FindFirstObjectByType<Volume>();
        if (_volume.profile.TryGet<ChromaticAberration>(out var ca))
        {
            _chromatic = ca;
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
        if (Managers.Ability.GetCurrentLevel(Define.AbilityType.Passive_PlayerShield) <= 0 || Stat.maxDefence.TotalValue <= 0)
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

        // 4. 충전 완료!
        if (shieldCoolTime >= Stat.shieldChargeTime.TotalValue)
        {
            // 방어막을 꽉 채워주고 쿨타임을 0으로 초기화합니다.
            currentDefence = Stat.maxDefence.TotalValue;
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
            Stat.maxDefence.AddValue(shieldCount);

            // 최대치가 늘어났을 때 현재 쉴드가 없다면 즉시 채워줍니다.
            if (currentDefence <= 0)
            {
                currentDefence = Stat.maxDefence.TotalValue;
                shieldCoolTime = 0f; // 쿨타임 초기화
                OnStatusEvent();
            }
        }
    }
    #region 피격 및 사망
    public void OnDamage(float damage)
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
            // 방어막 히트 이벤트
            Managers.Effect.Play(EffectType.Screen_ShieldHit, Vector2.zero);
        }
        // 방어막 없으면
        else
        {
            currentHp -= damage;
            // 그냥 히트 이펙트
            //Managers.Effect.Play(EffectType.Screen_PlayerHit, Vector2.zero);
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
        float elapsed = 0;

        // 카메라 쉐이크
        mainCam.transform.DOShakePosition(0.2f, 0.5f, 20, 90f).SetUpdate(true);

        // 다시 PropertyBlock 부활!
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        // 주의: 우리가 아까 셰이더 그래프에서 만든 변수 이름 Reference ("_FlashColor")를 써야 합니다!
        string colorProp = "_FlashColor";

        bool isFlash = false;

        while (elapsed < Stat.hitCooldown)
        {
            if (sr != null)
            {
                sr.GetPropertyBlock(mpb);

                // 플래시 상태면 엄청 밝은 흰색, 아니면 그냥 원래 기본 색상(흰색)
                Color targetColor = isFlash ? new Color(2f, 2f, 2f, 1f) : Color.white;

                mpb.SetColor(colorProp, targetColor);
                sr.SetPropertyBlock(mpb);

                isFlash = !isFlash;
            }

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        // 무적 종료 시 원상 복구
        if (sr != null)
        {
            sr.GetPropertyBlock(mpb);
            mpb.SetColor(colorProp, Color.white);
            sr.SetPropertyBlock(mpb);
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
            Managers.Sound.Play(Define.SoundID.Sfx_BurstModeOn);
            
            StartCoroutine(BurstRoutine());
        }
        else
        {
        }
    }
    private IEnumerator BurstRoutine()
    {
        mainCam.DOOrthoSize(15.0f, 0.3f)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnUpdate(() =>
            {
                Managers.Map.UpdateMap();
                Managers.Effect.Play(EffectType.Screen_BurstMode, Vector3.zero);
            });

        Stat.speed.AddMultiplier(1.0f);
        Stat.reloadTime.SetForceZero(true);
        Stat.shotTime.AddMultiplier(-0.5f);

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

        mainCam.DOOrthoSize(12f, 0.3f)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnUpdate(() => Managers.Map.UpdateMap());

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
        _curStatus.shield = currentDefence;
        _curStatus.burst = currentBurst;
        _curStatus.maxBurst = Stat.maxBurstGuage.TotalValue;

        // 쉴드 쿨타임 비율 계산 (안전하게 Clamp01 적용)
        if (Stat.maxDefence.TotalValue > 0)
        {
            if (currentDefence > 0)
            {
                // 방어막이 꽉 차 있으면 슬라이더도 100%(1.0f)로 고정
                _curStatus.shieldCooldownRatio = 1.0f;
            }
            else
            {
                // 방어막이 없으면 쿨타임 비율 계산 (현재 쿨타임 / 최대 쿨타임)
                // Mathf.Clamp01을 써서 만약의 경우에도 게이지가 100%를 넘지 않게 방어합니다.
                _curStatus.shieldCooldownRatio = Mathf.Clamp01(shieldCoolTime / Stat.shieldChargeTime.TotalValue);
            }
        }
        else
        {
            // 패시브가 없으면 게이지 0%
            _curStatus.shieldCooldownRatio = 0f;
        }
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