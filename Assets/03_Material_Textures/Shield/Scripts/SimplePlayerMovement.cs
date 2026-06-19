namespace EnergyShield
{
	using UnityEngine;
	using UnityEngine.InputSystem;

	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(PlayerInput))]
	public class SimplePlayerMovement : MonoBehaviour
	{
		[Header("Movement Settings")]
		public float moveSpeed = 6f;

		private Rigidbody _rb;
		private PlayerInput _playerInput;
		private InputAction _moveAction;
		private Vector3 _moveDirection;

		private void Start()
		{
			_rb = GetComponent<Rigidbody>();
			_playerInput = GetComponent<PlayerInput>();

			// Find the "Move" action from the attached Player Input component.
			// Make sure your Input Actions asset has an action named "Move" (usually a Vector2).
			_moveAction = _playerInput.actions["Move"];

			// Freeze all rotations so the capsule doesn't fall over when moving
			_rb.constraints = RigidbodyConstraints.FreezeRotation;
		}

		private void Update()
		{
			// Check if the move action is valid to prevent errors
			if (_moveAction == null) return;

			// Read the Vector2 value from the Move action (e.g., WASD or Gamepad Left Stick)
			Vector2 inputVector = _moveAction.ReadValue<Vector2>();

			// Calculate direction relative to where the player is currently facing
			_moveDirection = (transform.right * inputVector.x + transform.forward * inputVector.y).normalized;
		}

		private void FixedUpdate()
		{
			// Apply movement velocity, but keep the current Y velocity for normal gravity falling
			Vector3 targetVelocity = _moveDirection * moveSpeed;
			_rb.linearVelocity = new Vector3(targetVelocity.x, _rb.linearVelocity.y, targetVelocity.z);
		}
	}
}