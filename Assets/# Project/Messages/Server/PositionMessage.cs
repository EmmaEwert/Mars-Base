namespace Game {
	using Net;
	using Unity.Mathematics;

	public class PositionMessage : ReliableMessage, IServerMessage {
		public int id;
		public float3 position;

		protected override int length => sizeof(int) + sizeof(float) * 3;

		public PositionMessage() { }
		public PositionMessage(Entity entity, float3 position) {
			this.id = entity.id;
			this.position = position;
		}

		public void Read(Reader reader) {
			reader.Read(out id);
			reader.Read(out position);
		}

		public void Write(Writer writer) {
			writer.Write(id);
			writer.Write(position);
		}
	}
}