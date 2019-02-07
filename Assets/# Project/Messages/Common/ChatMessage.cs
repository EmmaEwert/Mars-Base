namespace Game {
	using Net;

	public class ChatMessage : SimpleReliableMessage {
		public int id;
		public string text;
	}
}