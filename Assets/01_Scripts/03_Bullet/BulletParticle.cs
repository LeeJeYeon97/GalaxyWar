using System.Collections;
using UnityEngine;

public class BulletParticle : MonoBehaviour
{
    [Header("Effects")]
    public ParticleSystem projectilePS;
    public GameObject[] Detached;
    public GameObject flashEffect;
    public GameObject hitEffect;

    [Header("Components")]
    [SerializeField] protected Light lightSourse;

    [Header("Lifetime")]
    [SerializeField] protected float detachedLifeTime = 1f;

    [System.Serializable]
    protected class DetachedState
    {
        public GameObject obj;
        public Transform originalParent;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    protected DetachedState[] detachedStates;

    private void Awake()
    {
        SetupDetachedCache();
    }
    private void OnEnable()
    {
        if (lightSourse != null)
            lightSourse.enabled = true;

        if (projectilePS != null)
        {
            projectilePS.Clear(true);
            projectilePS.Play(true);
        }
    }
    private void SetupDetachedCache()
    {
        if (Detached == null || Detached.Length == 0)
            return;

        detachedStates = new DetachedState[Detached.Length];

        for (int i = 0; i < Detached.Length; i++)
        {
            GameObject obj = Detached[i];
            if (obj == null)
                continue;

            detachedStates[i] = new DetachedState
            {
                obj = obj,
                originalParent = obj.transform.parent,
                localPosition = obj.transform.localPosition,
                localRotation = obj.transform.localRotation,
                localScale = obj.transform.localScale
            };
        }
    }

    private void RestoreDetachedObjects()
    {
        if (detachedStates == null || detachedStates.Length == 0)
            return;

        for (int i = 0; i < detachedStates.Length; i++)
        {
            DetachedState state = detachedStates[i];
            if (state == null || state.obj == null)
                continue;

            Transform t = state.obj.transform;

            t.SetParent(state.originalParent, false);
            t.localPosition = state.localPosition;
            t.localRotation = state.localRotation;
            t.localScale = state.localScale;

            ParticleSystem[] systems = state.obj.GetComponentsInChildren<ParticleSystem>(true);
            for (int j = 0; j < systems.Length; j++)
            {
                ParticleSystem ps = systems[j];
                if (ps == null)
                    continue;

                ps.Clear(true);
                ps.Play(true);
            }
        }
    }

    public void SpawnHit(Vector2 hitPos, Vector2 hitNormal,BaseBulletStat stat)
    {
        if (stat == null)
            return;
        
        GameObject hitGo = Managers.Pool.Get(hitEffect).gameObject;
        if (hitGo != null)
        {

            hitGo.transform.position = hitPos;
            // 3. 방향 회전 (벽에서 튕겨 나오는 방향)
            // hitNormal이 0이 아닐 때만 회전 (Trigger 관통탄은 허공에서 터지므로 제외)
            if (hitNormal != Vector2.zero)
            {
                // ★ 핵심: 파티클의 발사구(Z축)를 벽의 법선(hitNormal) 방향으로 딱 맞춰줍니다!
                // 이렇게 하면 왼쪽 벽에 맞으면 오른쪽으로, 바닥에 맞으면 위쪽으로 파티클이 뿜어집니다.
                hitGo.transform.rotation = Quaternion.LookRotation(hitNormal);
            }
            else
            {
                // 관통탄처럼 허공에서 터질 때는 그냥 총알이 날아가던 반대 방향으로 터지게 해도 멋집니다.
                hitGo.transform.rotation = Quaternion.identity;
            }

            if (stat is ExplosionBulletStat expStat)
            {
                float radius = expStat.explosionRange.TotalValue;
                // 폭발 범위에 맞춰 파티클 스케일 뻥튀기!
                hitGo.transform.localScale = new Vector3(radius, radius, radius);
            }
        }
        ParticleSystem hitPS = hitGo.GetComponent<ParticleSystem>();
        if (hitPS != null)
        {
            hitPS.Clear(true);
            hitPS.Play(true);
        }
        ReleaseDetachedObjects();
    }

    public void SpawnShot(Vector2 shotDir, Vector2 shotPos)
    {
        GameObject flash = Managers.Pool.Get(flashEffect).gameObject;
        if (flash != null)
        {
            flash.transform.position = shotPos;
            flash.transform.rotation = Quaternion.LookRotation(shotDir);
        }
    }
    private void ReleaseDetachedObjects()
    {
        if (detachedStates == null || detachedStates.Length == 0)
            return;

        for (int i = 0; i < detachedStates.Length; i++)
        {
            DetachedState state = detachedStates[i];
            if (state == null || state.obj == null)
                continue;

            Transform t = state.obj.transform;
            t.SetParent(null, true);

            ParticleSystem[] systems = state.obj.GetComponentsInChildren<ParticleSystem>(true);
            for (int j = 0; j < systems.Length; j++)
            {
                ParticleSystem ps = systems[j];
                if (ps == null)
                    continue;

                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}

