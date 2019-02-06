using Sandbox.Net;

public class GiveMeTheCatsMessage : Message, IClientMessage {
	protected override int length => 0;

	public void Read(Reader reader) { }

	public void Write(Writer writer) { }
}