namespace Game {
	using UnityEngine;

	public class ServerActor : MonoBehaviour {
		public int id;

		void Update() {
			transform.Translate(Time.deltaTime, 0, 0);
			new ActorTransformMessage(id, transform.position).Broadcast();
		}
	}
}