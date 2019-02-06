namespace Sandbox.Net {
	public interface IServerMessage {
		void Read(Reader reader);
		void Write(Writer writer);
	}
}