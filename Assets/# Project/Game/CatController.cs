namespace Game {
	using Net;
	using Unity.Mathematics;
	using UnityEngine;
	using UnityEngine.EventSystems;

	public class CatController : MonoBehaviour {
		public float moveSpeed;
		public float jumpHeight;
		public float jumpDistance;
		public float variableJumpDistance;
		float2 velocity;
		bool grounded = false;

		void Update() {
			var Δt = Time.deltaTime;

			// Select this cat if nothing is selected.
			if (EventSystem.current.currentSelectedGameObject == null) {
				EventSystem.current.SetSelectedGameObject(gameObject);
			}

			// Handle inputs if this cat is selected.
			if (EventSystem.current.currentSelectedGameObject == gameObject) {
				// Determine horizontal movement.
				velocity.x = Input.GetAxis("Horizontal") * moveSpeed;

				// Determine vertical movement (jump)
				var v0 = (2 * jumpHeight * moveSpeed) / jumpDistance;
				if (grounded && Input.GetButtonDown("Jump")) {
					velocity.y = v0;
				}
			}

			// Determine gravity.
			float g;
			if (Input.GetButton("Jump") || velocity.y < 0) {
				g = (-2 * jumpHeight * moveSpeed * moveSpeed) / (jumpDistance * jumpDistance);
			} else {
				g = (-2 * jumpHeight * moveSpeed * moveSpeed) / (variableJumpDistance * variableJumpDistance);
			}

			// Update position and vertical velocity.
			var position = new float3(transform.position) + new float3(velocity * Δt + new float2(0, 0.5f) * g * Δt * Δt, 0);
			velocity += new float2(0, 1) * g * Δt;

			// Kill vertical position and velocity if we're on the ground.
			if (position.y <= 0) {
				position.y = 0;
				velocity.y = 0;
				grounded = true;
			} else {
				grounded = false;
			}

			// Sync to other players.
			if (math.any(position != new float3(transform.position))) {
				// FIXME: Don't use connection ID
				new PlayerTransformMessage {
					id = FindObjectOfType<Net.Client>().connectionID,
					position = position
				}.Send();
			}

			// Apply position.
			transform.position = position;
		}
	}
}