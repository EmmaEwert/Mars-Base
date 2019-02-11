namespace Game.Server {
	using Unity.Mathematics;

	public class PathSelectSystem : ComponentSystem {
		const float MoveSpeed = 1f;
		const float ε = 1f / 64f;
		
		Random random = new Random(1);

		void OnUpdate(Entity entity, Position position, Velocity velocity) {
			// Exclude cats with a set destination
			if (Manager.GetComponent<Destination>(entity)) {
				return;
			}
			var nodes = Manager.FindAll(typeof(PathNode));
			var node = nodes[random.NextInt(nodes.Count)];
			var destination = Manager.AddComponent<Destination>(entity);
			destination.nodeID = node.id;
		}
	}
}

