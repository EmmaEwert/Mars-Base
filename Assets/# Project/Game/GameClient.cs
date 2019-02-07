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
		EntityManager manager;

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

		void SetSprite(SpriteMessage message) {
			var manager = FindObjectOfType<EntityManager>();
			var entity = manager.Entity(message.id);
			var renderer = manager.AddComponent<SpriteRenderer>(entity);
			renderer.sprite = Resources.Load<Sprite>($"Sprites/{message.sprite}");
		}

		void SetPosition(PositionMessage message) {
			var entity = manager.Entity(message.id);
			if (!entity) { return; }
			entity.transform.position = message.position;
		}
		
		void Start() {
			Net.Client.Listen<ConnectServerMessage>(StartClientGame);
			Net.Client.Listen<PlayerTransformMessage>(SyncTransform);
			Net.Client.Listen<SpriteMessage>(SetSprite);
			Net.Client.Listen<PositionMessage>(SetPosition);
			var client = gameObject.AddComponent<Net.Client>();
			client.remoteIP = remoteIP;
			Net.Client.playerName = playerName;
			manager = FindObjectOfType<EntityManager>();
		}
	}
}