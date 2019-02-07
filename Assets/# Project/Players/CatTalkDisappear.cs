namespace Game {
	using UnityEngine;

	public class CatTalkDisappear : MonoBehaviour {
		float lifetime = 3f;

		void Update() { 
			lifetime -= Time.deltaTime;
			if (lifetime < 0f) {
				Destroy(gameObject);
			}
		}
	}
}