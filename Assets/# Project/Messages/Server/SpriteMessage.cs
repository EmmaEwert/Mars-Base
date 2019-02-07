namespace Game {
	using Net;

	public class SpriteMessage : ReliableMessage, IServerMessage {
		public int id;
		public string sprite;

		protected override int length => StringSize(sprite);

		public SpriteMessage() { }
		public SpriteMessage(Entity entity, string sprite) {
			this.id = entity.id;
			this.sprite = sprite;
		}

		public void Read(Reader reader) {
			reader.Read(out id);
			reader.Read(out sprite);
		}

		public void Write(Writer writer) {
			writer.Write(id);
			writer.Write(sprite);
		}
	}
}