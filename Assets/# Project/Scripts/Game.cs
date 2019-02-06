using System;
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

	public void Host() {
		gameObject.AddComponent<Server>();
		server = true;
		var random = new Unity.Mathematics.Random(1); 
		var strings = new [] {
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
		for (var i = 0; i < strings.Length; ++i) {
			serverNPCs.Add((new float3(random.NextInt() % 128, 1, 0), strings[i]));
		}
		var client = gameObject.AddComponent<Client>();
		client.remoteIP = Server.localIP.ToString();
		Client.playerName = playerName;
	}

	public void Join() {
		var client = gameObject.AddComponent<Client>();
		client.remoteIP = remoteIP;
		Client.playerName = playerName;
	}

	void OnApplicationQuit() {
		Client.Stop();
		if (server) {
			Server.Stop();
		}
	}

	void StartClientGame(ConnectServerMessage message) {
		DontDestroyOnLoad(gameObject);
		SceneManager.LoadScene("Game", LoadSceneMode.Single);
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void SyncTransform(PlayerTransformMessage message) {
		if (message.id != Client.id) {
			if (!players.TryGetValue(message.id, out var player)) {
				players[message.id] = Instantiate(remotePlayerPrefab);
			}
			players[message.id].transform.position = message.position;
		}
	}

	void BroadcastTransform(PlayerTransformMessage message) {
		message.Broadcast();
	}

	void SendCatsToClient(GiveMeTheCatsMessage message) {
		for (var i = 0; i < serverNPCs.Count; ++i) {
			new CatSpawnMessage(i, serverNPCs[i].pos, serverNPCs[i].text).Send(message.connection);
		}
	}

	void ReceiveCat(CatSpawnMessage message) {
		var npc = Instantiate(npcPrefab);
		npc.transform.position = message.position;
		npc.name = message.text;
		npcs.Add(message.id, npc);
	}

	void BroadcastCatTalk(CatTalkMessage message) {
		message.Broadcast();
	}

	void ReceiveCatTalk(CatTalkMessage message) {
		var canvas = GameObject.Find("World Space Canvas");
		var talk = Instantiate(catTalkPrefab);
		talk.GetComponentInChildren<TextMeshProUGUI>().text = message.text;
		talk.transform.SetParent(canvas.transform);
		talk.transform.position = message.position;
		talk.name = $"{message.text} Talk";
	}

	void Start() {
		Client.Listen<ConnectServerMessage>(StartClientGame);
		Client.Listen<PlayerTransformMessage>(SyncTransform);
		Client.Listen<CatSpawnMessage>(ReceiveCat);
		Client.Listen<CatTalkMessage>(ReceiveCatTalk);
		Server.Listen<PlayerTransformMessage>(BroadcastTransform);
		Server.Listen<GiveMeTheCatsMessage>(SendCatsToClient);
		Server.Listen<CatTalkMessage>(BroadcastCatTalk);
	}

	void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
		new GiveMeTheCatsMessage().Send();
	}
}