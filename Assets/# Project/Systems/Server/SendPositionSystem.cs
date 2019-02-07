namespace Game.Server {
	using Net;

	public class SendPositionSystem : ComponentSystem {
		void SendPosition(ConnectClientMessage message) {
			var entities = Manager.FindAll(typeof(Position));
			foreach (var entity in entities) {
				var position = entity.GetComponent<Position>().value;
				new PositionMessage(entity, position).Send(message.connection);
			}
		}

		void Start() {
			Net.Server.Listen<ConnectClientMessage>(SendPosition);
		}
	}
}