namespace EnergyShield
{
	using UnityEngine;
	[RequireComponent(typeof(Renderer))]
	public class ShieldHitPulse : MonoBehaviour
	{
		private Renderer _renderer;
		private Material _shieldMaterial;
		private float _currentStrength;
		private float _lerpSpeed;

		private void Start()
		{
			_renderer = GetComponent<Renderer>();
			// Create an instance of the material so it doesn't modify the project asset
			_shieldMaterial = _renderer.material;
		}

		// Called by the bullet when it hits the shield
		public void TriggerPulse(Vector3 hitPos, float maxStrength, float radius, float lerpSpeed)
		{
			if (_shieldMaterial == null) return;

			_lerpSpeed = Mathf.Max(0.01f, lerpSpeed);
			_currentStrength = Mathf.Max(_currentStrength, maxStrength); // Refresh strength

			// Send data to your Custom/EnergyShield shader
			_shieldMaterial.SetVector("_HitPos", hitPos);
			_shieldMaterial.SetFloat("_HitRadius", radius);
			_shieldMaterial.SetFloat("_HitStrength", _currentStrength);
		}

		private void Update()
		{
			// Smoothly fade out the hit effect over time
			if (_shieldMaterial != null && _currentStrength > 0f)
			{
				_currentStrength = Mathf.Lerp(_currentStrength, 0f, Time.deltaTime * _lerpSpeed);
				_shieldMaterial.SetFloat("_HitStrength", _currentStrength);
			}
		}
	}
}