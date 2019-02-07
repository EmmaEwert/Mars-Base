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

	///<summary>Spawn an actor as the authoritative client.</summary>
	public void Spawn(string text, float3 position) {
		var actor = actors[nextActorID] = Instantiate(prefab, position, Quaternion.identity);
		actor.gameObject.AddComponent<ServerActor>().id = nextActorID;
		actor.text = text;
		new ActorMessage(nextActorID, actor).Broadcast();
		++nextActorID;
	}

	///<summary>Spawn a cat on a non-authoritative client.</summary>
	void Spawn(ActorMessage message) {
		if (!server) {
			actors[message.id] = Instantiate(prefab, message.position, Quaternion.identity);
			actors[message.id].text = message.text;
		}
	}

	///<summary>Sync a cat transform on a non-authoritative client.</summary>
	void Sync(ActorTransformMessage message) {
		if (!server && actors.TryGetValue(message.id, out var actor)) {
			actor.transform.position = message.position;
		}
	}

	///<summary>Send all known cats from the authoritative client to the connecting client.</summary>
	void SendAll(ConnectClientMessage message) {
		foreach (var pair in actors) {
			new ActorMessage(pair.Key, pair.Value).Send(message.connection);
		}
	}

	///<summary>Set up listeners.</summary>
	void Start() {
		Client.Listen<ActorMessage>(Spawn);
		Client.Listen<ActorTransformMessage>(Sync);
		Server.Listen<ConnectClientMessage>(SendAll);
	}
}