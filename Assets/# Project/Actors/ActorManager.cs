using System.Collections.Generic;
using Net;
using Unity.Mathematics;
using UnityEngine;

public class ActorManager : MonoBehaviour {
	public static ActorManager instance => FindObjectOfType<ActorManager>();

	public Actor prefab;
	Dictionary<int, Actor> actors = new Dictionary<int, Actor>();
	int nextActorID = 0;

	bool server => GetComponent<Server>() != null;

	public void Spawn(string text, float3 position) {
		var actor = actors[nextActorID] = Instantiate(prefab, position, Quaternion.identity);
		actor.gameObject.AddComponent<ServerActor>().id = nextActorID;
		actor.text = text;
		new ActorMessage(nextActorID, actor).Broadcast();
		++nextActorID;
	}

	void Spawn(ActorMessage message) {
		if (!server) {
			actors[message.id] = Instantiate(prefab, message.position, Quaternion.identity);
			actors[message.id].text = message.text;
		}
	}

	void Sync(ActorTransformMessage message) {
		if (!server && actors.TryGetValue(message.id, out var actor)) {
			actor.transform.position = message.position;
		}
	}

	void SendAll(ConnectClientMessage message) {
		foreach (var pair in actors) {
			new ActorMessage(pair.Key, pair.Value).Send(message.connection);
		}
	}

	void Start() {
		Client.Listen<ActorMessage>(Spawn);
		Client.Listen<ActorTransformMessage>(Sync);
		Server.Listen<ConnectClientMessage>(SendAll);
	}
}