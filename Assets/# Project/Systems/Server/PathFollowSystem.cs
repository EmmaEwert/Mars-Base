namespace Game.Server {
	using Unity.Mathematics;
	using UnityEngine;

	public class PathFollowSystem : ComponentSystem {
		const float MoveSpeed = 1f;
		const float ε = 1f / 64f;

		void OnUpdate(Entity entity, Position position, Velocity velocity, Destination destination) {
			var nodeEntity = Manager.Entity(destination.nodeID);
			var nodePosition = Manager.GetComponent<Position>(nodeEntity);
			var difference = nodePosition.value - position.value;
			var Δ = math.sqrt(math.dot(difference, difference));
			if (Δ < ε) {
				Manager.RemoveComponent<Destination>(entity);
				velocity.value = new float3(0);
			} else {
				velocity.value = math.normalize(difference) * math.min(MoveSpeed, Δ / Time.deltaTime);
			}
		}
	}
}
