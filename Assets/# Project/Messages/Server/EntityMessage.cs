namespace Game {
	using Net;

	public class EntityMessage : SimpleReliableMessage {
		public int id;
		public string name;
	}
}