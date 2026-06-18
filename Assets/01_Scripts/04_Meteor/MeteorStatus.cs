using System.Collections;
using UnityEngine;
using static Define;

public class MeteorStatus : MonoBehaviour
{
    private MeteorController _controller;

    public bool HasAuraBuff { get; private set; } = false;
    private float _auraBuffEndTime = 0f;

    private Coroutine _freezeCoroutine;
    private Coroutine _slowCoroutine;
    private Coroutine _burnCoroutine;

    public GameObject auraBuffEffectPrefab;
    //private GameObject auraBuff;
    public GameObject shockDeBuffEffectPrefab;
    private GameObject shockDeBuffEffect;
    public bool HasShockDebuff { get; private set; } = false;

    // [변경점] 얼음 장판 예약 변수들
    public bool hasIcePuddleMark = false;
    public float icePuddleDamage = 0f;
    public float icePuddleRadius = 0f;
    public float icePuddleSlowPercent = 0f;

    private void Awake()
    {
        _controller = GetComponent<MeteorController>();

        if (auraBuffEffectPrefab != null)
        {
            //auraBuff = Managers.Resource.Instantiate(auraBuffEffectPrefab);
            //auraBuff.transform.SetParent(this.transform, false);
            //
            //// 수정: 부모의 정중앙을 기준으로 Z축만 -3 당깁니다.
            //auraBuff.transform.localPosition = new Vector3(0, 0, -3);
            //auraBuff.SetActive(false);
        }
        if (shockDeBuffEffectPrefab != null)
        {
            shockDeBuffEffect = Managers.Resource.Instantiate(shockDeBuffEffectPrefab);
            //  핵심: false 옵션 추가
            shockDeBuffEffect.transform.SetParent(this.transform, false);

            //  수정: localPosition 사용
            shockDeBuffEffect.transform.localPosition = new Vector3(0, 0, -3);
            shockDeBuffEffect.SetActive(false);
        }
    }

    public void Init()
    {
        HasAuraBuff = false;
        HasShockDebuff = false;

        // [변경점] 얼음 장판 마커 초기화
        hasIcePuddleMark = false;
        icePuddleDamage = 0f;
        icePuddleRadius = 0f;
        icePuddleSlowPercent = 0f;

        _auraBuffEndTime = 0f;
        //auraBuff.SetActive(false);
        shockDeBuffEffect.SetActive(false);

        if (_freezeCoroutine != null) StopCoroutine(_freezeCoroutine);
        if (_slowCoroutine != null) StopCoroutine(_slowCoroutine);
        if (_burnCoroutine != null) StopCoroutine(_burnCoroutine);

        // [추가된 핵심 코드] 변수 안에 남은 코루틴 찌꺼기를 완벽히 지워줍니다!
        _freezeCoroutine = null;
        _slowCoroutine = null;
        _burnCoroutine = null;

        _controller.Visual.ReturnColor();

    }

    private void Update()
    {
        //if (HasAuraBuff && Time.time > _auraBuffEndTime)
        //{
        //    HasAuraBuff = false;
        //    if (auraBuff != null)
        //    {
        //        auraBuff.SetActive(false);
        //    }
        //}
    }

    public void ReceiveAuraBuff(float duration)
    {
        //_auraBuffEndTime = Time.time + duration;
        //if (!HasAuraBuff)
        //{
        //    HasAuraBuff = true;
        //    //_controller.Visual.SetColor(Color.yellow);
        //    if(auraBuff != null)
        //    {
        //        auraBuff.SetActive(true);
        //    }
        //}
    }

    #region 감전
    public void ApplyShock()
    {
        if (!gameObject.activeInHierarchy) return;

        HasShockDebuff = true;
        if (shockDeBuffEffect != null)
        {
            shockDeBuffEffect.SetActive(true);
        }
        // 시각적 효과 (예: 찌릿찌릿한 보라색/자주색 느낌)
        //_controller.Visual.SetColor(new Color(0.8f, 0.2f, 1f));
    }
    // 2. 감전 터트리기
    // 감전 상태였다면 상태를 지우고 true를 반환, 아니면 false 반환
    public bool ConsumeShock()
    {
        if (HasShockDebuff)
        {
            HasShockDebuff = false;
            if (shockDeBuffEffect != null)
            {
                shockDeBuffEffect.SetActive(false);
            }
            //_controller.Visual.ReturnColor(); // 원래 색상으로 복구
            return true;
        }
        return false;
    }
    #endregion

    #region 슬로우/빙결

    public void ApplySlow(float slowPercent, float duration)
    {
        if (!gameObject.activeInHierarchy) return;
        if (_slowCoroutine != null)
        {
            StopCoroutine(_slowCoroutine);
            _controller.Movement.currentSpeed.SubMultiplier(-slowPercent);
            _controller.Movement.UpdateVelocity();
        }
        _slowCoroutine = StartCoroutine(CoSlowRoutine(slowPercent, duration));
    }

    private IEnumerator CoSlowRoutine(float slowPercent, float duration)
    {
        _controller.Movement.currentSpeed.AddMultiplier(-slowPercent);
        _controller.Movement.UpdateVelocity();
        _controller.Visual.SetColor(new Color(0.5f, 0.8f, 1f));

        yield return new WaitForGameTime(duration);

        _controller.Movement.currentSpeed.SubMultiplier(-slowPercent);
        _slowCoroutine = null;
        _controller.Visual.ReturnColor();
    }

    public void ApplyFreeze(float freezeDuration, float slowPercent, float slowDuration)
    {
        if (!gameObject.activeInHierarchy) return;
        if (_freezeCoroutine != null)
        {
            StopCoroutine(_freezeCoroutine);
            _controller.Movement.currentSpeed.SetForceZero(false);
            _controller.Movement.UpdateVelocity();
        }
        _freezeCoroutine = StartCoroutine(CoFreezeRoutine(freezeDuration, slowPercent, slowDuration));
    }

    private IEnumerator CoFreezeRoutine(float duration, float slowPercent, float slowDuration)
    {
        _controller.Movement.currentSpeed.SetForceZero(true);
        _controller.Movement.UpdateVelocity();
        _controller.Visual.SetColor(new Color(0.2f, 0.8f, 1f));

        yield return new WaitForGameTime(duration);

        _controller.Movement.currentSpeed.SetForceZero(false);
        _controller.Movement.UpdateVelocity();
        _freezeCoroutine = null;
        _controller.Visual.ReturnColor();
        hasIcePuddleMark = false;

        ApplySlow(slowPercent, slowDuration);
    }

    // 총알이 5레벨일 때 이 몬스터에게 얼음 파편 폭발을 예약하는 함수
    // [변경점] 파편 마커 함수 대신 장판 마커 함수로 변경
    public void AddIcePuddleMark(float damage, float radius, float slowPercent)
    {
        hasIcePuddleMark = true;
        icePuddleDamage = damage;
        icePuddleRadius = radius;
        icePuddleSlowPercent = slowPercent;
    }

    #endregion

    #region 화상
    public void ApplyBurn(float burnDamage, float duration, float tickTime)
    {
        if (!gameObject.activeInHierarchy) return;
        if (_burnCoroutine != null) StopCoroutine(_burnCoroutine);
        _burnCoroutine = StartCoroutine(CoBurnRoutine(burnDamage, duration, tickTime));
    }

    private IEnumerator CoBurnRoutine(float tickDamage, float duration, float tickTime)
    {
        float timer = 0f;
        while (timer < duration && gameObject.activeInHierarchy)
        {
            if (Managers.Game.currentGameState == GameState.Pause)
            {
                yield return null;
                continue;
            }

            _controller.Visual.SetColor(new Color(1f, 0.4f, 0f));
            _controller.OnDamage(tickDamage);

            yield return new WaitForSeconds(tickTime);
            timer += tickTime;
        }
        _burnCoroutine = null;
        _controller.Visual.ReturnColor();
    }
    #endregion

    public Color GetCurrentStatusColor()
    {
        //if (HasAuraBuff) return Color.yellow;
        if (_burnCoroutine != null) return new Color(1f, 0.4f, 0f);
        if (_freezeCoroutine != null) return new Color(0.2f, 0.8f, 1f);
        if (_slowCoroutine != null) return new Color(0.5f, 0.8f, 1f);
        return Color.white;
    }
}