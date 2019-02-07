using System.Collections.Generic;
using Net;
using TMPro;
using UnityEngine;

public class GameClient : MonoBehaviour {
	public string remoteIP;
	public string playerName;
	GameObject catTalkPrefab => Resources.Load("Cat Talk") as GameObject;
	GameObject npcPrefab => Resources.Load("Remote Player") as GameObject;
	GameObject localPlayerPrefab => Resources.Load("Local Player") as GameObject;
	GameObject remotePlayerPrefab => Resources.Load("Remote Player") as GameObject;
	Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
	public Dictionary<int, GameObject> npcs = new Dictionary<int, GameObject>();

	///<summary>Request all cats on the server and deactivate the main menu.</summary>
	void StartClientGame(ConnectServerMessage message) {
		GameObject.Find("Main Menu").SetActive(false);
		Instantiate(localPlayerPrefab);
	}

	///<summary>Update position of a single remote player.</summary>
	void SyncTransform(PlayerTransformMessage message) {
		// FIXME: Should have player ID instead, connectionID is for internal use
		if (message.id != GetComponent<Client>().connectionID) {
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
		var canvas = GameObject.Find("World/Canvas");
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
		var client = gameObject.AddComponent<Client>();
		client.remoteIP = remoteIP;
		Client.playerName = playerName;
	}
}