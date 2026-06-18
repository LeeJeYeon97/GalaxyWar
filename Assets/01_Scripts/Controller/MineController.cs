using System.Collections;
using UnityEngine;

public class MineController : MonoBehaviour
{
    [Header("Components")]
    public GameObject spriteNormal; // 평상시 지뢰 이미지
    public GameObject spriteBlink;  // 빨간불이 들어온 지뢰 이미지
    public CircleCollider2D mineCollider;

    public GameObject explodeEffect;

    [Header("Explosion Settings")]
    public float explosionRadius = 2.5f;    // 폭발 시 데미지를 줄 광역 범위
    public float blinkInterval = 0.5f;      // 깜빡이는 속도 (초)
    public float curDamage = 0;

    private bool _isActive = false;         // 지뢰 활성화 여부

    // 날아가는 연출 대신, 지정된 위치에 즉시 설치하는 함수
    public void PlantMine(Vector2 pos, float damage, float radius)
    {
        transform.position = pos;
        _isActive = true;

        // 즉시 충돌체(센서) 활성화
        if (mineCollider != null)
        {
            mineCollider.enabled = true;
            mineCollider.isTrigger = true;
        }
        explosionRadius = radius;
        curDamage = damage;
        // 초기 스프라이트 상태 세팅 (기본 켜고, 불빛 끄고)
        if (spriteNormal != null) spriteNormal.SetActive(true);
        if (spriteBlink != null) spriteBlink.SetActive(false);

        // 깜빡임 코루틴 시작!
        StartCoroutine(CoBlinkRoutine());
    }

    //  2개의 오브젝트를 번갈아가며 껐다 켜는 핵심 연출 코루틴
    private IEnumerator CoBlinkRoutine()
    {
        while (_isActive)
        {
            // 설정한 시간만큼 대기
            yield return new WaitForSeconds(blinkInterval);

            // 두 스프라이트의 켜짐/꺼짐 상태를 반대로 뒤집음 (토글)
            if (spriteNormal != null && spriteBlink != null)
            {
                bool isNormalActive = spriteNormal.activeSelf;
                spriteNormal.SetActive(!isNormalActive);
                spriteBlink.SetActive(isNormalActive);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isActive) return;

        // 메테오가 밟았을 때 폭발!
        if (collision.CompareTag("Meteor"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        _isActive = false;
        if (mineCollider != null) mineCollider.enabled = false;

        int layerMask = LayerMask.GetMask("Meteor", "Boss");

        // 레이어 마스크를 추가로 넘겨주면 쓰레기 데이터(땅, 플레이어 등)는 아예 감지하지 않습니다!
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius, layerMask);
        foreach (Collider2D enemy in hitEnemies)
        {
            //  2단계 방어: IDamageable을 찾고, null이 아닐 때만 데미지를 줍니다!
            // (콜라이더가 자식에 있을 수 있으므로 GetComponentInParent 사용 권장)
            IDamageable target = enemy.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                target.OnDamage(curDamage);
            }
        }

        // 2. 폭발 파티클 생성
        GameObject effect = Managers.Resource.Instantiate(explodeEffect);
        effect.transform.position = transform.position;
        effect.transform.localScale = new Vector3(explosionRadius, explosionRadius, explosionRadius);

        Managers.Sound.Play(Define.SoundID.Sfx_MineExplosion);

        // 3. 지뢰 본체 삭제 (풀링 반환)
        Managers.Resource.Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}