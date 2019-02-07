namespace Game.Server {
	using System;
	using Net;

	public class SendSpriteSystem : ComponentSystem {
		Type[] subjects = { typeof(RenderSprite) };

		void SendSprite(ConnectClientMessage message) {
			var entities = Manager.FindAll(subjects);
			foreach (var entity in entities) {
				var sprite = entity.GetComponent<RenderSprite>().resource;
				new SpriteMessage(entity, sprite).Send(message.connection);
			}
		}

		void Start() {
			Net.Server.Listen<ConnectClientMessage>(SendSprite);
		}
	}
}