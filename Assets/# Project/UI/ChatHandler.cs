namespace Game {
	using UnityEngine;
	using TMPro;
	using Net;
	using System.Collections.Generic;
	using System.Linq;
	using Unity.Mathematics;

	[RequireComponent(typeof(TextMeshProUGUI))]
	public class ChatHandler : MonoBehaviour {
		List<string> messages = new List<string>();

		TextMeshProUGUI textMesh => GetComponent<TextMeshProUGUI>();

		public void Send(string text) {
			if (text == string.Empty) { return; }
			new ChatMessage(FindObjectOfType<Net.Client>().connectionID, text).Send();
		}

		void Add(ChatMessage message) {
			var name = FindObjectOfType<Net.Client>().playerConnections[message.id];
			messages.Add($"{name}: {message.text}");
			textMesh.text = string.Empty;
			foreach (var text in messages.Skip(math.max(messages.Count - 10, 0))) {
				textMesh.text += $"\n{text}";
			}
		}

		void Broadcast(ChatMessage message) {
			message.Broadcast();
		}

		void Start() {
			Net.Client.Listen<ChatMessage>(Add);
			Net.Server.Listen<ChatMessage>(Broadcast);
		}
	}
}