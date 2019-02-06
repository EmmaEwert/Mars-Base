namespace Sandbox.Net {
	public interface IClientMessage {
		void Read(Reader reader);
		void Write(Writer writer);
	}
}
