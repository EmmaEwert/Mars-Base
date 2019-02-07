namespace Game.Client {
	using System.Collections.Generic;
	using UnityEngine;

	public class EntityManager : MonoBehaviour {
		Dictionary<int, Entity> entities = new Dictionary<int, Entity>();

		public T AddComponent<T>(Entity entity) where T : Component {
			return entity.gameObject.AddComponent<T>();
		}

		public Entity Entity(int id) {
			if (entities.TryGetValue(id, out var entity)) {
				return entity;
			}
			return null;
		}

		void Instantiate(EntityMessage message) {
			var entity = new GameObject(message.name).AddComponent<Entity>();
			entity.transform.SetParent(transform);
			entity.id = message.id;
			entities[entity.id] = entity;
		}

		void Start() {
			Net.Client.Listen<EntityMessage>(Instantiate);
		}
	}
}