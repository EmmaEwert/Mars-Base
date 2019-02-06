using Sandbox.Net;
using Unity.Mathematics;

public class PlayerTransformMessage : Message, IServerMessage, IClientMessage {
	public int id;
	public float3 position;

	protected override int length => sizeof(int) + sizeof(float) * 3;

	public PlayerTransformMessage() { }
	public PlayerTransformMessage(int id, float3 position) {
		this.id = id;
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
