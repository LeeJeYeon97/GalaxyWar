using GooglePlayGames.BasicApi;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class PlayerCombat : MonoBehaviour
{
    private PlayerController _player; // 본체 참조

    [Header("Weapon Settings")]
    public Transform gunTransform;      // 미니건 포신
    public float gunRotateSpeed = 20f;  // 회전 속도
    public Transform _bulletPos;        // 총알 발사구 위치

    [Header("Combat State")]
    public List<BulletController> bullets = new List<BulletController>();
    public bool isReloading = false; // 본체(Burst)에서 접근할 수 있도록 public

    private float _lastShotTime;
    private float _lastHomingShotTime;
    private Vector2 _currentAimDir;
    private Coroutine _reloadCoroutine;

    // 자동 조준 관련
    private GameObject _target;
    private float _targetTimer = 0f;
    private float _targetUpdateInterval = 0.1f;


    public event Action<List<BulletController>> OnReload;
    public void Init(PlayerController player)
    {
        _player = player;
        isReloading = false;
        _lastHomingShotTime = 0f;
        // 게임 시작 시 첫 장전
        Reload();
    }

    private void Update()
    {
        // 플레이 중이 아니면 무기 작동 중지
        if (_player == null ||
            _player.currentState != PlayerState.Playing ||
            Managers.Game.currentGameState != GameState.Playing)
            return;

        FindTarget();
        Shoot();
        HomingShot();
    }

    private void FixedUpdate()
    {
        if (_player == null || 
            _player.currentState != PlayerState.Playing ||
            Managers.Game.currentGameState != GameState.Playing) 
            return;

        RotateGun();
    }

    #region Auto Aim
    private void FindTarget()
    {
        _targetTimer += Time.deltaTime;
        if (_targetTimer < _targetUpdateInterval) return;
        _targetTimer = 0f;

        float minDistance = Mathf.Infinity;
        _target = null;

        // 1. 추적할 레이어만 골라내기 (메테오와 적 레이어를 포함)
        int layerMask = LayerMask.GetMask("Meteor", "Boss");

        //  2. 레이어 마스크를 사용하여 오버랩 탐색 (최적화!)
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            transform.position,
            _player.Stat.shotRange.TotalValue,
            layerMask
        );

        foreach (Collider2D col in colliders)
        {
            // 3. 메테오인지 보스인지 묻지 말고, IDamageable 인터페이스만 확인!
            IDamageable damageable = col.GetComponent<IDamageable>();

            // 인터페이스가 있고, 활성화된 상태라면 타겟으로 고려
            if (damageable != null && col.gameObject.activeInHierarchy)
            {
                float distance = Vector2.Distance(transform.position, col.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    _target = col.gameObject;
                }
            }
        }
    }
    #endregion

    #region Shooting
    private void Shoot()
    {
        if (isReloading) return;
        if (Managers.Data.GameData.noAttack) return;
        if (bullets.Count <= 0)
        {
            if (_reloadCoroutine == null)
                _reloadCoroutine = StartCoroutine(CoReload());
            return;
        }

        if (Time.time - _lastShotTime >= _player.Stat.shotTime.TotalValue)
        {
            BulletController bullet = bullets[0];
            bullets.RemoveAt(0);

            if (bullet != null)
            {
                bullet.transform.position = _bulletPos.position;
                Vector2 shootDir = transform.up;

                if (_target != null && _target.activeInHierarchy)
                {
                    shootDir = (_target.transform.position - _bulletPos.position).normalized;
                }

                _currentAimDir = shootDir;
                Managers.Sound.Play(SoundID.Sfx_PlayerShot, Sound.Sfx);

                if (_player.Stat.isMultiShotEnabled && _player.Stat.multiShotCount.TotalValue > 1)
                {
                    float multiShotChance = UnityEngine.Random.Range(0f, 100f);
                    if (multiShotChance <= _player.Stat.multiShotChance.TotalValue)
                    {
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


                Managers.Event.PostEvent<List<BulletController>>(ActionEvent.PlayerShot, bullets);
                _lastShotTime = Time.time;
            }
        }
    }

    private void HomingShot()
    {
        if (!_player.Stat.isHomingShotEnabled) return;
        if (Managers.Game.activeMeteors.Count <= 0) return;

        HomingBulletStat homingStat = Managers.Stat.GetBulletStat(Define.BulletType.HomingBullet) as HomingBulletStat;
        if (homingStat == null) return;

        _lastHomingShotTime += Time.deltaTime;

        if (_lastHomingShotTime >= homingStat.homingShotDelay.TotalValue)
        {
            Camera mainCam = Camera.main;

            // ====================================================
            // 핵심 변경: 0~3 중 랜덤한 숫자를 뽑아 스폰 방향 결정
            // ====================================================
            int randomSide = UnityEngine.Random.Range(0, 4);

            Vector3 viewportPos = Vector3.zero;
            Vector2 initialDir = Vector2.zero;

            switch (randomSide)
            {
                case 0: // 위 (Top)
                    viewportPos = new Vector3(UnityEngine.Random.Range(0.1f, 0.9f), 1.1f, 0);
                    initialDir = Vector2.down;
                    break;
                case 1: // 아래 (Bottom)
                    viewportPos = new Vector3(UnityEngine.Random.Range(0.1f, 0.9f), -0.1f, 0);
                    initialDir = Vector2.up;
                    break;
                case 2: // 왼쪽 (Left)
                    viewportPos = new Vector3(-0.1f, UnityEngine.Random.Range(0.1f, 0.9f), 0);
                    initialDir = Vector2.right;
                    break;
                case 3: // 오른쪽 (Right)
                    viewportPos = new Vector3(1.1f, UnityEngine.Random.Range(0.1f, 0.9f), 0);
                    initialDir = Vector2.left;
                    break;
            }

            // Z값은 카메라 깊이로 동일하게 맞춰줌
            viewportPos.z = Mathf.Abs(mainCam.transform.position.z);
            Vector2 outOfScreenPos = mainCam.ViewportToWorldPoint(viewportPos);

            // ====================================================
            // 프리팹 생성 및 발사
            // ====================================================
            GameObject go = Managers.Resource.Instantiate(homingStat.originalPrefabs);
            BulletController bullet = go.GetComponent<BulletController>();

            bullet.SetBullet(homingStat);

            // 위에서 스위치문으로 결정된 방향(initialDir)과 위치(outOfScreenPos) 적용!
            bullet.Shot(initialDir, outOfScreenPos);

            // 쿨타임 초기화
            _lastHomingShotTime = 0;
        }
    }

    private void FireMultiShot(BulletController mainBullet, Vector2 baseShootDir)
    {
        float baseAngle = Mathf.Atan2(baseShootDir.y, baseShootDir.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - (_player.Stat.multiShotAngle / 2f);
        float angleStep = _player.Stat.multiShotAngle / (_player.Stat.multiShotCount.TotalValue - 1);

        for (int i = 0; i < _player.Stat.multiShotCount.TotalValue; i++)
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
    #endregion

    #region Reloading
    private IEnumerator CoReload()
    {
        isReloading = true;

        Managers.Event.PostEvent<float>(ActionEvent.ReloadStart, _player.Stat.reloadTime.TotalValue);
        Managers.Sound.Play(Define.SoundID.Sfx_Reloading);

        yield return new WaitForGameTime(_player.Stat.reloadTime.TotalValue);

        Reload();

        _reloadCoroutine = null;
    }

    public void Reload()
    {
        foreach (var bullet in bullets)
        {
            Managers.Resource.Destroy(bullet.gameObject);
        }
        bullets.Clear();

        int reloadCount = Mathf.FloorToInt(_player.Stat.reloadCount.TotalValue);
        for (int i = 0; i < reloadCount; i++)
        {
            BaseBulletStat stat;
            if (_player._isBurst)
            {
                stat = Managers.Stat.GetBulletStat(Define.BulletType.BurstBullet);
            }
            else
            {
                stat = Managers.Stat.GetRandomBulletStat();
            }

            if (stat == null) return;

            GameObject go = Managers.Resource.Instantiate(stat.originalPrefabs);
            BulletController bullet = go?.GetComponent<BulletController>();

            if (bullet == null) return;

            bullet.SetBullet(stat);
            bullets.Add(bullet);
        }

        Managers.Event.PostEvent(ActionEvent.ReloadEnd, bullets);
        isReloading = false;
    }

    // 버스트 모드에서 리로드를 강제로 끊기 위해 사용하는 함수
    public void CancelReload()
    {
        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }
        isReloading = false;
    }
    #endregion

    #region Gun Rotation
    private void RotateGun()
    {
        if (gunTransform == null) return;

        Vector2 aimDir = transform.up;
        if (_target != null && _target.activeInHierarchy)
        {
            aimDir = (_target.transform.position - gunTransform.position).normalized;
        }

        float targetAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        float finalAngle = targetAngle - 90f;

        Quaternion targetRotation = Quaternion.Euler(0, 0, finalAngle);
        gunTransform.rotation = Quaternion.Lerp(gunTransform.rotation, targetRotation, Time.fixedDeltaTime * gunRotateSpeed);
    }
    #endregion


    #region Gizmos
    // 플레이어 오브젝트를 클릭(Select)했을 때만 씬 뷰에 그려주는 함수입니다.
    // (항상 보이게 하려면 OnDrawGizmos() 로 이름을 바꾸시면 됩니다!)
    private void OnDrawGizmosSelected()
    {
        if (_player == null) return;
        // 게임을 실행하기 전(에디터 상태)에는 Stat이 아직 할당되지 않아 
        // Null 에러가 날 수 있으므로 안전장치를 걸어줍니다.
        if (_player.Stat == null || _player.Stat.shotRange == null) return;

        // 눈에 잘 띄도록 반투명한 붉은색(또는 초록색)으로 색상을 설정합니다.
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

        // 내 위치를 중심으로 shotRange만큼의 크기를 가진 원(선)을 그립니다.
        Gizmos.DrawWireSphere(transform.position, _player.Stat.shotRange.TotalValue);
    }
    #endregion

}