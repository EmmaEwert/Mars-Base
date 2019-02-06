using Sandbox.Net;
using Unity.Mathematics;
using UnityEngine;

public class CatController : MonoBehaviour {
	public float moveSpeed;
	public float jumpHeight;
	public float jumpDistance;
	public float variableJumpDistance;
	float2 velocity;
	bool grounded = false;

	void Update() {
		var Δt = Time.deltaTime;

		// Determine horizontal movement
		velocity.x = Input.GetAxis("Horizontal") * moveSpeed;


		// Determine vertical movement (jump)
		var v0 = (2 * jumpHeight * moveSpeed) / jumpDistance;
		if (grounded && Input.GetButtonDown("Jump")) {
			velocity.y = v0;
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
			new PlayerTransformMessage(Client.id, position).Send();
		}

		// Apply position.
		transform.position = position;

		// Show chat message for a cat close by?
		GameObject closestNPC = null;
		foreach (var npc in FindObjectOfType<Game>().npcs.Values) {
			var diff = new float3(npc.transform.position - transform.position);
			if (math.dot(diff, diff) < 8f) {
				if (closestNPC != null) {
					var diff2 = new float3(closestNPC.transform.position - transform.position);
					if (math.dot(diff2, diff2) > math.dot(diff, diff)) {
						closestNPC = npc;
					}
				} else {
					closestNPC = npc;
				}
			}
		}
		if (closestNPC != null) {
			if (GameObject.Find($"{closestNPC.name} Talk") == null) {
				new CatTalkMessage(closestNPC.transform.position, closestNPC.name).Send();
			}
		}
	}
}
