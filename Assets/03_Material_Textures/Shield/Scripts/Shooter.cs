namespace EnergyShield
{
	using UnityEngine;

	public class Shooter : MonoBehaviour
	{
		[Header("Target Configuration")]
		[Tooltip("The specific Transform (object) the bullets will shoot at.")]
		public Transform targetPoint;

		[Header("Fire Points")]
		[Tooltip("Assign the 4 empty GameObjects here where the bullets will spawn from.")]
		public Transform[] firePoints;

		[Header("Shooting Settings")]
		public GameObject bulletPrefab;
		public float fireRate = 0.5f;

		private float _nextFireTime;

		private void Update()
		{
			// Check if it is time to fire again based on the fire rate
			if (Time.time >= _nextFireTime)
			{
				Shoot();
				_nextFireTime = Time.time + fireRate;
			}
		}

		private void Shoot()
		{
			// Prevent errors if the target point or bullet prefab is missing
			if (targetPoint == null || bulletPrefab == null) return;

			// Iterate through each fire point and spawn a bullet
			foreach (Transform firePoint in firePoints)
			{
				if (firePoint == null) continue;

				// Instantiate the bullet at the fire point's current position
				GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

				// Immediately rotate the bullet so its forward direction faces the target
				bullet.transform.LookAt(targetPoint.position);
			}
		}
	}
}