namespace Game {
	using Net;

	public class SpriteMessage : SimpleReliableMessage {
		public int id;
		public string sprite;
	}
}