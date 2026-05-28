using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorVisual : MonoBehaviour
{
    private MeteorController _controller;
    // 1. MeshRenderer 대신 SpriteRenderer로 교체합니다.
    private SpriteRenderer _spriteRenderer;
    private Coroutine _flashCoroutine;

    //  MPB를 캐싱해두고 재사용하기 위한 변수
    private MaterialPropertyBlock _mpb;

    // Shader.PropertyToID를 쓰면 문자열 연산을 매번 하지 않아 성능이 더 좋습니다.
    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");

    private void Awake()
    {
        _controller = GetComponent<MeteorController>();

        // 메테오 이미지(Sprite)가 부모에 있는지 자식에 있는지에 따라 맞춰줍니다.
        // 만약 자식 오브젝트에 이미지가 있다면 GetComponentInChildren<SpriteRenderer>()를 쓰세요!
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Awake에서 딱 한 번만 할당합니다.
        _mpb = new MaterialPropertyBlock();
    }

    public void Init()
    {
        // 오브젝트 풀에서 꺼낼 때 혹시 하얗게 남아있을지 모르는 플래시를 초기화
        ResetFlashColor();
        ReturnColor();
    }

    public void SetColor(Color color)
    {
        // 2. 복잡한 MaterialPropertyBlock 없이 직관적으로 색상을 바꿉니다.
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = color;
        }
    }
    public void ReturnColor()
    {
        if (_spriteRenderer != null && _controller.Status != null)
        {
            SetColor(_controller.Status.GetCurrentStatusColor());
        }
    }

    public void PlayHitFlash()
    {
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);

        // 풀링 과정에서 오브젝트가 꺼져있을 때 코루틴이 돌면 에러가 날 수 있으므로 방어 코드 추가
        if (gameObject.activeInHierarchy)
        {
            _flashCoroutine = StartCoroutine(CoHitFlash());
        }
    }

    // 2. 피격 시 번쩍이는 발광(Glow) 효과만 MPB를 통해 셰이더로 넘겨줍니다!
    private IEnumerator CoHitFlash()
    {
        if (_spriteRenderer == null) yield break;

        //  플래시 ON (HDR 컬러로 눈부시게!)
        _spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(FlashColorID, new Color(3f, 3f, 3f, 1f));
        _spriteRenderer.SetPropertyBlock(_mpb);

        yield return new WaitForGameTime(0.1f);

        //  플래시 OFF (원상 복구)
        ResetFlashColor();
    }

    private void ResetFlashColor()
    {
        if (_spriteRenderer != null)
        {
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(FlashColorID, Color.white); // 플레이어 셰이더 기준 기본 중립 색상
            _spriteRenderer.SetPropertyBlock(_mpb);
        }
    }
}