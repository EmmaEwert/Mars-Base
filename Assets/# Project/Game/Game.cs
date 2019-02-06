using System.Collections.Generic;
using Sandbox.Net;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour {
	public string playerName { get; set; } = "Emma";
	public string remoteIP { get; set; } = "82.180.25.150";

	public GameObject catTalkPrefab;
	public GameObject npcPrefab;
	public GameObject remotePlayerPrefab;
	Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
	public Dictionary<int, GameObject> npcs = new Dictionary<int, GameObject>();
	List<(float3 pos, string text)> serverNPCs = new List<(float3, string)>();
	bool server;
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

	// Server stuff.

	///<summary>Start a server and a client.</summary>
	public void Host() {
		server = true;
		gameObject.AddComponent<Server>();
		var random = new Unity.Mathematics.Random(1); 
		for (var i = 0; i < catStrings.Length; ++i) {
			serverNPCs.Add((new float3(random.NextInt() % 128, 1, 0), catStrings[i]));
		}
		var client = gameObject.AddComponent<Client>();
		client.remoteIP = Server.localIP.ToString();
		Client.playerName = playerName;
	}

	///<summary>Relay player positions to all clients.</summary>
	void BroadcastTransform(PlayerTransformMessage message) {
		message.Broadcast();
	}

	///<summary>Send all cats and their text lines to client upon request.</summary>
	void SendCatsToClient(GiveMeTheCatsMessage message) {
		for (var i = 0; i < serverNPCs.Count; ++i) {
			new CatSpawnMessage(i, serverNPCs[i].pos, serverNPCs[i].text).Send(message.connection);
		}
	}

	///<summary>Relay cat talk to all clients.</summary>
	void BroadcastCatTalk(CatTalkMessage message) {
		message.Broadcast();
	}

	// Client stuff.

	///<summary>Start a client.</summary>
	public void Join() {
		var client = gameObject.AddComponent<Client>();
		client.remoteIP = remoteIP;
		Client.playerName = playerName;
	}

	///<summary>Switch client scene after initial connection to server has been established.</summary>
	void StartClientGame(ConnectServerMessage message) {
		DontDestroyOnLoad(gameObject);
		SceneManager.LoadScene("Game", LoadSceneMode.Single);
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	///<summary>Update position of a single remote player.</summary>
	void SyncTransform(PlayerTransformMessage message) {
		if (message.id != Client.id) {
			if (!players.TryGetValue(message.id, out var player)) {
				players[message.id] = Instantiate(remotePlayerPrefab);
			}
			players[message.id].transform.position = message.position;
		}
	}

	///<summary>Spawn a single cat; bulk response from server upon a GiveMeTheCatsMessage</summary>
	void ReceiveCat(CatSpawnMessage message) {
		var npc = Instantiate(npcPrefab);
		npc.transform.position = message.position;
		npc.name = message.text;
		npcs.Add(message.id, npc);
	}

	///<summary>Spawn message prefab each time an npc cat is "talked with".</summary>
	void ReceiveCatTalk(CatTalkMessage message) {
		var canvas = GameObject.Find("World Space Canvas");
		var talk = Instantiate(catTalkPrefab);
		talk.GetComponentInChildren<TextMeshProUGUI>().text = message.text;
		talk.transform.SetParent(canvas.transform);
		talk.transform.position = message.position;
		talk.name = $"{message.text} Talk";
	}

	///<summary>Request all cats on the server once the game scene has loaded.</summary>
	void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
		new GiveMeTheCatsMessage().Send();
	}

	// Common stuff.

	///<summary>Set up all the message listeners.</summary>
	void Start() {
		Client.Listen<ConnectServerMessage>(StartClientGame);
		Client.Listen<PlayerTransformMessage>(SyncTransform);
		Client.Listen<CatSpawnMessage>(ReceiveCat);
		Client.Listen<CatTalkMessage>(ReceiveCatTalk);
		Server.Listen<PlayerTransformMessage>(BroadcastTransform);
		Server.Listen<GiveMeTheCatsMessage>(SendCatsToClient);
		Server.Listen<CatTalkMessage>(BroadcastCatTalk);
	}

	///<summary>Clean up network client and server internals before quitting.</summary>
	void OnApplicationQuit() {
		Client.Stop();
		if (server) {
			Server.Stop();
		}
	}
}