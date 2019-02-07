namespace Net {
	public class ConnectClientMessage : ReliableMessage, IClientMessage {
		public string name;

		protected override int length => StringSize(name);

		public ConnectClientMessage() { }
		public ConnectClientMessage(string name) {
			this.name = name;
		}

		public void Read(Reader reader) {
			reader.Read(out name);
		}

		public void Write(Writer writer) {
			writer.Write(name);
		}
	}
}