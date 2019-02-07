namespace Sandbox.Net {
	public class JoinMessage : ReliableMessage, IServerMessage {
		public int id;
		public string name;

		public bool local => id == Client.connectionID;
		protected override int length => sizeof(int) + StringSize(name);

		public JoinMessage() { }
		public JoinMessage(int id, string name) {
			this.id = id;
			this.name = name;
		}

		public void Read(Reader reader) {
			reader.Read(out id);
			reader.Read(out name);
		}

		public void Write(Writer writer) {
			writer.Write(id);
			writer.Write(name);
		}
	}
}