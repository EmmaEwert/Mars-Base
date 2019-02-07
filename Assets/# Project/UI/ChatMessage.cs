namespace Game {
	using Net;

	public class ChatMessage : ReliableMessage, IServerMessage, IClientMessage {
		public int id;
		public string text;

		protected override int length => sizeof(int) + StringSize(text);

		public ChatMessage() { }
		public ChatMessage(int id, string text) {
			this.id = id;
			this.text = text;
		}

		public void Read(Reader reader) {
			reader.Read(out id);
			reader.Read(out text);
		}

		public void Write(Writer writer) {
			writer.Write(id);
			writer.Write(text);
		}
	}
}