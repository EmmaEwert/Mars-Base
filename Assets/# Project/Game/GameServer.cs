using System.Collections.Generic;
using Net;
using Unity.Mathematics;
using UnityEngine;

public class GameServer : MonoBehaviour {
	List<(float3 pos, string text)> npcs = new List<(float3, string)>();
	string[] catStrings = {
		"Hum hum hummm...",
		"Nice day out today!",
		"Did you see the sunrise?",
		"There should be a mewteor shower tonight!",
		"Can't wait for the next shipment from Purth...",
		"...",
		"Hmm.",
		"Well, that's not right...",
		"Hello.",
		"Hello!",
		"Hey.",
		"Hey!",
		"Heya.",
		"Hi.",
		"Hi!",
		"Hiya!",
		"Greetings.",
		"Nice to see you.",
		"This Miaurtian dust gets everywhere...",
		"Chu-!",
		"Hungryyyyy...",
		"Can we pause for food?",
		"I wish the rations included snacks...",
		"Do you think we could grow mice ?",
		"Sleepy...",
		"Tired...",
		"Tiiiiired...",
		"I want a naaaaap.",
		"Not now.",
		"Talk later ?",
		"Sorry, busy.",
		"...do you ever wish we had enough rovers to race them?",
		"Eugh... hearing the dust storms against the ice shield makes my fur stand on end.",
	};

	///<summary>Relay player positions to all clients.</summary>
	void BroadcastTransform(PlayerTransformMessage message) {
		message.Broadcast();
	}

	///<summary>Relay cat talk to all clients.</summary>
	void BroadcastCatTalk(CatTalkMessage message) {
		message.Broadcast();
	}

	///<summary>Send all cats and their text lines to client upon request.</summary>
	void SendCatsToClient(ConnectClientMessage message) {
		for (var i = 0; i < npcs.Count; ++i) {
			new CatSpawnMessage(i, npcs[i].pos, npcs[i].text).Send(message.connection);
		}
	}

	void Start() {
		Server.Listen<ConnectClientMessage>(SendCatsToClient);
		Server.Listen<PlayerTransformMessage>(BroadcastTransform);
		Server.Listen<CatTalkMessage>(BroadcastCatTalk);
		gameObject.AddComponent<Server>();
		var random = new Unity.Mathematics.Random(1); 
		for (var i = 0; i < catStrings.Length; ++i) {
			npcs.Add((new float3(random.NextInt() % 128, 1, 0), catStrings[i]));
		}
	}
}