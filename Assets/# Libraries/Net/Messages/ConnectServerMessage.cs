namespace Sandbox.Net {
	internal class ConnectServerMessage : ReliableMessage, IServerMessage {
		public int id;

		protected override int length => sizeof(int);

		public ConnectServerMessage() { }
		public ConnectServerMessage(int connection) {
			this.id = connection;
		}

		public void Read(Reader reader) {
			reader.Read(out id);
		}

		public void Write(Writer writer) {
			writer.Write(id);
		}
	}
}