namespace Net {
	internal class PingMessage : Message, IServerMessage, IClientMessage {
		protected override int length => 0;

		public void Read(Reader reader) { }
		public void Write(Writer writer) { }
	}
}