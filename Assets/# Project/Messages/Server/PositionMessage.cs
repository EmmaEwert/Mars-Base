namespace Game {
	using Net;
	using Unity.Mathematics;

	public class PositionMessage : SimpleMessage {
		public int id;
		public float3 position;
	}
}