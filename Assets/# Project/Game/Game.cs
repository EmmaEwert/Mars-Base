namespace Game {
	using Net;
	using UnityEngine;

	public class Game : MonoBehaviour {
		public string playerName { get; set; } = "Emma";
		public string remoteIP { get; set; } = "82.180.25.150";

		///<summary>Start a server and a client.</summary>
		public void Host() {
			gameObject.AddComponent<Server.GameServer>();
			remoteIP = IP.local.ToString();
			Join();
		}

		///<summary>Start a client.</summary>
		public void Join() {
			var client = gameObject.AddComponent<Client.GameClient>();
			client.remoteIP = remoteIP;
			client.playerName = playerName;
		}
	}
}