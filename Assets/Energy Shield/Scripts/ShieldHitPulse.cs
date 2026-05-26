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
            _shieldMaterial = _renderer.material;

            //  [추가] 쉴드가 무조건 플레이어보다 앞에 그려지도록 렌더링 서열을 강제로 높여줍니다.
            // 플레이어의 Order in Layer가 0이라면, 쉴드는 그보다 무조건 높게 설정하세요.
            _renderer.sortingOrder = 10;
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