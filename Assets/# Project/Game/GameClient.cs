namespace Game.Client {
	using System.Collections.Generic;
	using Net;
	using TMPro;
	using UnityEngine;

	public class GameClient : MonoBehaviour {
		public string remoteIP;
		public string playerName;
		GameObject catTalkPrefab => Resources.Load("Cat Talk") as GameObject;
		GameObject localPlayerPrefab => Resources.Load("Local Player") as GameObject;
		GameObject remotePlayerPrefab => Resources.Load("Remote Player") as GameObject;
		Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();

		///<summary>Request all cats on the server and deactivate the main menu.</summary>
		void StartClientGame(ConnectServerMessage message) {
			GameObject.Find("Main Menu").SetActive(false);
			Instantiate(localPlayerPrefab);
		}

		///<summary>Update position of a single remote player.</summary>
		void SyncTransform(PlayerTransformMessage message) {
			// FIXME: Should have player ID instead, connectionID is for internal use
			if (message.id != GetComponent<Net.Client>().connectionID) {
				if (!players.TryGetValue(message.id, out var player)) {
					players[message.id] = Instantiate(remotePlayerPrefab);
				}
				players[message.id].transform.position = message.position;
			}
		}

		///<summary>Spawn message prefab each time an npc cat is "talked with".</summary>
		void ReceiveCatTalk(CatTalkMessage message) {
			var canvas = GameObject.Find("World/Canvas");
			var talk = Instantiate(catTalkPrefab);
			talk.GetComponentInChildren<TextMeshProUGUI>().text = message.text;
			talk.transform.SetParent(canvas.transform);
			talk.transform.position = message.position;
			talk.name = message.text;
		}

		void SetSprite(SpriteMessage message) {
			var manager = FindObjectOfType<EntityManager>();
			var entity = manager.Entity(message.id);
			var renderer = manager.AddComponent<SpriteRenderer>(entity);
			renderer.sprite = Resources.Load<Sprite>($"Sprites/{message.sprite}");
		}

		void SetPosition(PositionMessage message) {
			var manager = FindObjectOfType<EntityManager>();
			var entity = manager.Entity(message.id);
			entity.transform.position = message.position;
		}
		
		void Start() {
			Net.Client.Listen<ConnectServerMessage>(StartClientGame);
			Net.Client.Listen<PlayerTransformMessage>(SyncTransform);
			Net.Client.Listen<CatTalkMessage>(ReceiveCatTalk);
			Net.Client.Listen<SpriteMessage>(SetSprite);
			Net.Client.Listen<PositionMessage>(SetPosition);
			var client = gameObject.AddComponent<Net.Client>();
			client.remoteIP = remoteIP;
			Net.Client.playerName = playerName;
		}
	}
}