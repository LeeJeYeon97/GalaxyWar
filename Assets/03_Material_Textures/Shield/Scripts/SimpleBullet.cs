namespace EnergyShield
{
	using UnityEngine;

	public class SimpleBullet : MonoBehaviour
	{
		[Header("Movement")]
		public float speed = 20f;
		public float lifeTime = 3f;

		[Header("Shield Hit Effect Settings")]
		public float hitMaxStrength = 5f;
		public float hitRadius = 2f;
		public float hitLerpSpeed = 10f;

		private void Start()
		{
			// Destroy the bullet after 'lifeTime' seconds
			Destroy(gameObject, lifeTime);
		}

		private void Update()
		{
			// Move forward
			transform.Translate(Vector3.forward * speed * Time.deltaTime);
		}

		// Triggered when the bullet's collider hits another trigger/collider
		private void OnTriggerEnter(Collider other)
		{
			// Check if the object we hit has the shield pulse script
			ShieldHitPulse shieldPulse = other.GetComponent<ShieldHitPulse>();

			if (shieldPulse != null)
			{
				// Trigger the shader animation at the bullet's current position
				shieldPulse.TriggerPulse(transform.position, hitMaxStrength, hitRadius, hitLerpSpeed);
			}

			// Destroy the bullet after hitting something
			Destroy(gameObject);
		}
	}
}