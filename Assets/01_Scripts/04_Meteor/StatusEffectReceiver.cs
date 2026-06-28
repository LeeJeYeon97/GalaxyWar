using System.Collections;
using Unity.Services.CloudSave.Models.Data.Player;
using UnityEngine;
using static Define;

public class StatusEffectReceiver : MonoBehaviour
{
    private IStatusTarget _target;

    public bool HasAuraBuff { get; private set; } = false;
    public bool HasShockDebuff { get; private set; } = false;

    private Coroutine _freezeCoroutine;
    private Coroutine _slowCoroutine;
    private Coroutine _burnCoroutine;

    public GameObject shockDeBuffEffectPrefab;
    private GameObject shockDeBuffEffect;

    public bool hasIcePuddleMark = false;
    public float icePuddleDamage = 0f;
    public float icePuddleRadius = 0f;
    public float icePuddleSlowPercent = 0f;

    private void Awake()
    {
        _target = GetComponent<IStatusTarget>();

        if (_target == null)
        {
            Debug.LogError($"{gameObject.name}에 IStatusTarget을 구현한 컴포넌트가 없습니다!");
        }

        if (shockDeBuffEffectPrefab != null)
        {
            shockDeBuffEffect = Managers.Resource.Instantiate(shockDeBuffEffectPrefab);
            shockDeBuffEffect.transform.SetParent(this.transform, false);
            shockDeBuffEffect.transform.localPosition = new Vector3(0, 0, -3);
            shockDeBuffEffect.SetActive(false);
        }
    }

    public void Init()
    {
        HasAuraBuff = false;
        HasShockDebuff = false;
        hasIcePuddleMark = false;

        if (shockDeBuffEffect != null) shockDeBuffEffect.SetActive(false);

        if (_freezeCoroutine != null) StopCoroutine(_freezeCoroutine);
        if (_slowCoroutine != null) StopCoroutine(_slowCoroutine);
        if (_burnCoroutine != null) StopCoroutine(_burnCoroutine);

        _freezeCoroutine = null;
        _slowCoroutine = null;
        _burnCoroutine = null;

        UpdateStatusColor(); // 초기화 시 색상 원상복구
    }

    // =========================================================
    // [핵심 추가] 중앙 집중형 색상 관리 함수
    // =========================================================
    private void UpdateStatusColor()
    {
        if (_target == null) return;

        // 우선순위 1: 빙결 (가장 강력한 제어기이므로 최우선 표시)
        if (_freezeCoroutine != null)
        {
            _target.SetStatusColor(new Color(0.2f, 0.8f, 1f)); // 진한 하늘색
        }
        // 우선순위 2: 화상
        else if (_burnCoroutine != null)
        {
            _target.SetStatusColor(new Color(1f, 0.4f, 0f)); // 주황색
        }
        // 우선순위 3: 슬로우
        else if (_slowCoroutine != null)
        {
            _target.SetStatusColor(new Color(0.5f, 0.8f, 1f)); // 연한 하늘색
        }
        // 걸린 상태 이상이 아무것도 없을 때
        else
        {
            _target.ResetStatusColor();
        }
    }

    #region 감전
    public void ApplyShock()
    {
        if (HasShockDebuff == true) return;

        HasShockDebuff = true;
        shockDeBuffEffect.SetActive(true);
    }

    public void ShockDebuffOff()
    {
        if (HasShockDebuff == false) return;

        HasShockDebuff = false;
        shockDeBuffEffect.SetActive(false);
    }

    // [추가] 감전 데미지 증폭 등에 사용될 소비 함수
    public bool ConsumeShock()
    {
        if (HasShockDebuff)
        {
            ShockDebuffOff();
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
            _target.SubSpeedMultiplier(-slowPercent);
        }
        _slowCoroutine = StartCoroutine(CoSlowRoutine(slowPercent, duration));
    }

    private IEnumerator CoSlowRoutine(float slowPercent, float duration)
    {
        _target.AddSpeedMultiplier(-slowPercent);
        UpdateStatusColor(); // 코루틴 시작 시 색상 업데이트 갱신

        yield return new WaitForGameTime(duration);

        _target.SubSpeedMultiplier(-slowPercent);
        _slowCoroutine = null;

        UpdateStatusColor(); // 코루틴 종료 시 색상 업데이트 갱신
    }

    public void ApplyFreeze(float freezeDuration, float slowPercent, float slowDuration)
    {
        if (!gameObject.activeInHierarchy) return;
        if (_freezeCoroutine != null)
        {
            StopCoroutine(_freezeCoroutine);
            _target.SetForceZeroSpeed(false);
        }
        _freezeCoroutine = StartCoroutine(CoFreezeRoutine(freezeDuration, slowPercent, slowDuration));
    }

    private IEnumerator CoFreezeRoutine(float duration, float slowPercent, float slowDuration)
    {
        _target.SetForceZeroSpeed(true);
        UpdateStatusColor(); // 코루틴 시작 시 색상 업데이트 갱신

        yield return new WaitForGameTime(duration);

        _target.SetForceZeroSpeed(false);
        _freezeCoroutine = null;
        hasIcePuddleMark = false;

        UpdateStatusColor(); // 코루틴 종료 시 색상 업데이트 갱신

        ApplySlow(slowPercent, slowDuration);
    }

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
        UpdateStatusColor(); // 시작 즉시 색상 업데이트 갱신
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

            _target.TakeStatusDamage(tickDamage);

            yield return new WaitForSeconds(tickTime);
            timer += tickTime;
        }
        _burnCoroutine = null;

        UpdateStatusColor(); // 종료 시 색상 갱신
    }
    #endregion
}