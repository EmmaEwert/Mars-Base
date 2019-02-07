using Net;
using Unity.Mathematics;

public class CatSpawnMessage : ReliableMessage, IServerMessage {
	public int id;
	public float3 position;
	public string text;

	protected override int length => sizeof(int) + sizeof(float) * 3 + StringSize(text);

	public CatSpawnMessage() { }
	public CatSpawnMessage(int id, float3 position, string text) {
		this.id = id;
		this.position = position;
		this.text = text;
	}

	public void Read(Reader reader) {
		reader.Read(out id);
		reader.Read(out position);
		reader.Read(out text);
	}

	public void Write(Writer writer) {
		writer.Write(id);
		writer.Write(position);
		writer.Write(text);
	}
}