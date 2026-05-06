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
    private GameObject auraBuff;
    private void Awake()
    {
        _controller = GetComponent<MeteorController>();

        if (auraBuffEffectPrefab != null)
        {
            auraBuff = Managers.Resource.Instantiate(auraBuffEffectPrefab);
            auraBuff.transform.SetParent(this.transform);
            auraBuff.transform.position = new Vector3(0, 0, -3);
            auraBuff.SetActive(false);
        }
    }

    public void Init()
    {
        HasAuraBuff = false;
        _auraBuffEndTime = 0f;
        if (_freezeCoroutine != null) StopCoroutine(_freezeCoroutine);
        if (_slowCoroutine != null) StopCoroutine(_slowCoroutine);
        if (_burnCoroutine != null) StopCoroutine(_burnCoroutine);
    }

    private void Update()
    {
        if (HasAuraBuff && Time.time > _auraBuffEndTime)
        {
            HasAuraBuff = false;
            if (auraBuff != null)
            {
                auraBuff.SetActive(false);
            }
        }
    }

    public void ReceiveAuraBuff(float duration)
    {
        _auraBuffEndTime = Time.time + duration;
        if (!HasAuraBuff)
        {
            HasAuraBuff = true;
            //_controller.Visual.SetColor(Color.yellow);
            if(auraBuff != null)
            {
                auraBuff.SetActive(true);
            }
        }
    }

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

        ApplySlow(slowPercent, slowDuration);
    }

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

    public Color GetCurrentStatusColor()
    {
        if (HasAuraBuff) return Color.yellow;
        if (_burnCoroutine != null) return new Color(1f, 0.4f, 0f);
        if (_freezeCoroutine != null) return new Color(0.2f, 0.8f, 1f);
        if (_slowCoroutine != null) return new Color(0.5f, 0.8f, 1f);
        return Color.white;
    }
}