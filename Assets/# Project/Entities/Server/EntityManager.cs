namespace Game.Server {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Net;
	using UnityEngine;

	public class EntityManager : MonoBehaviour {
		Dictionary<int, Entity> entities = new Dictionary<int, Entity>();
		Dictionary<Type, HashSet<int>> components = new Dictionary<Type, HashSet<int>> {
			{ typeof(Entity), new HashSet<int>() }
		};
		int nextID = 0;

		internal Entity Create(string name = "") {
			var entity = new GameObject(name).AddComponent<Entity>();
			entity.transform.SetParent(transform);
			entity.id = nextID;
			entities[nextID++] = entity;
			components[typeof(Entity)].Add(entity.id);
			new EntityMessage { id = entity.id, name = name }.Broadcast();
			return entity;
		}

		internal Entity Entity(int id) {
			return entities[id];
		}

		internal T AddComponent<T>(Entity entity) where T : Component {
			var component = entity.gameObject.AddComponent<T>();
			if (!components.TryGetValue(typeof(T), out var set)) {
				set = components[typeof(T)] = new HashSet<int>();
			}
			set.Add(entity.id);
			return component;
		}

		internal T GetComponent<T>(Entity entity) where T : Component {
			var component = entity.gameObject.GetComponent<T>();
			return component;
		}

		internal void RemoveComponent<T>(Entity entity) where T : Component {
			Destroy(entity.gameObject.GetComponent<T>());
			if (components.TryGetValue(typeof(T), out var set)) {
				set.Remove(entity.id);
			}
		}

		internal List<Entity> FindAll(params Type[] types) {
			var set = new HashSet<int>();
			for (var i = 0; i < types.Length; ++i) {
				if (this.components.TryGetValue(types[i], out var components)) {
					if (i == 0) {
						set.UnionWith(components);
					} else {
						set.IntersectWith(components);
					}
				} else {
					return new List<Entity>();
				}
			}
			return set.Select(id => entities[id]).ToList();
		}

		void SendAll(ConnectClientMessage message) {
			foreach (var entity in entities.Values) {
				new EntityMessage { id = entity.id, name = entity.name }.Send(message.connection);
			}
		}

		void Awake() {
			Net.Server.Listen<ConnectClientMessage>(SendAll);
		}
	}
}