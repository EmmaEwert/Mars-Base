namespace Game {
	using Net;
	using Unity.Mathematics;

	public class PlayerTransformMessage : SimpleMessage {
		public int id;
		public float3 position;
	}
}