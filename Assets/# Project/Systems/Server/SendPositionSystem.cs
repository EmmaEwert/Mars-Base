namespace Game.Server {
	using Net;
	using Unity.Mathematics;

	public class SendPositionSystem : ComponentSystem {
		void SendPosition(ConnectClientMessage message) {
			var entities = Manager.FindAll(typeof(Position));
			foreach (var entity in entities) {
				var position = entity.GetComponent<Position>().value;
				new PositionMessage { id = entity.id, position = position }.Send(message.connection);
			}
		}

		void Start() {
			Net.Server.Listen<ConnectClientMessage>(SendPosition);
		}

		void OnUpdate(Entity entity, Position position, Velocity _) {
			if (math.any(position.value != new float3(entity.transform.position))) {
				new PositionMessage { id = entity.id, position = position.value }.Broadcast();
			}
		}
	}
}