using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
    public float currentBurst;
    
    // 이벤트 발생용
    private PlayerStatusEvent _curStatus = new PlayerStatusEvent();
    private Volume _volume;
    private ChromaticAberration _chromatic;

    public void Init()
    {

        mainCam = Camera.main;

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
    }
    public void SetState(PlayerState state)
    {
        currentState = state;
    }

    
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

        mainCam.DOOrthoSize(9.6f, 0.3f).SetEase(Ease.OutCubic).SetUpdate(true).OnUpdate(() => Managers.Map.UpdateMap());

        // 버스트 종료 시 재장전 복구
        if (Combat != null)
        {
            Combat.CancelReload();
            Combat.isReloading = true;
            Combat.Reload();
        }

        Debug.Log("버스트 모드 해제");
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