namespace Game.Server {
	using UnityEngine;

	public class MoveSystem : ComponentSystem {
		void OnUpdate(Position position, Velocity velocity) {
			position.value += velocity.value * Time.deltaTime;
		}
	}
}