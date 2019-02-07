namespace Game {
	using Net;

	public class EntityMessage : ReliableMessage, IServerMessage {
		public int id;
		public string name;

		protected override int length => sizeof(int) + StringSize(name);

		public EntityMessage() { }
		public EntityMessage(Entity entity) {
			this.id = entity.id;
			this.name = entity.name;
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