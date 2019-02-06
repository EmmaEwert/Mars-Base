using Sandbox.Net;
using Unity.Mathematics;

public class CatTalkMessage : ReliableMessage, IClientMessage, IServerMessage {
	public float3 position;
	public string text;

	protected override int length => sizeof(float) * 3 + StringSize(text);

	public CatTalkMessage() { }
	public CatTalkMessage(float3 position, string text) {
		this.position = position;
		this.text = text;
	}

	public void Read(Reader reader) {
		reader.Read(out position);
		reader.Read(out text);
	}

	public void Write(Writer writer) {
		writer.Write(position);
		writer.Write(text);
	}
}