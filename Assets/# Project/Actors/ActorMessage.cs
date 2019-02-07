using Net;
using Unity.Mathematics;

public class ActorMessage : ReliableMessage, IServerMessage {
	public int id;
	public float3 position;
	public string text;

	protected override int length => sizeof(int) + StringSize(text);

	public ActorMessage() { }
	public ActorMessage(int id, Actor actor) {
		this.id = id;
		this.position = actor.transform.position;
		this.text = actor.text;
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