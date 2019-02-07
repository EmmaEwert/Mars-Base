namespace Net {
	using System.Collections.Generic;
	using System.Linq;
	using UnityEngine;

	public abstract class ReliableMessage : Message {
		static int[] Delays = { 1, 1, 2, 3, 5, 8, 13, 21 };
		static float time;

		// Client
		static int incomingSequence;
		static int outgoingSequence;
		static Dictionary<int, ReliableMessage> clientMessages = new Dictionary<int, ReliableMessage>();
		static Dictionary<int, ReliableMessage> waitingOnClient = new Dictionary<int, ReliableMessage>();

		// Server
		static Dictionary<int, int> incomingSequences = new Dictionary<int, int>();
		static Dictionary<int, int> outgoingSequences = new Dictionary<int, int>();
		static Dictionary<(int connection, int sequence), ReliableMessage> serverMessages = new Dictionary<(int, int), ReliableMessage>();
		static Dictionary<(int connection, int sequence), ReliableMessage> waitingOnServer = new Dictionary<(int connection, int sequence), ReliableMessage>();

		int resends;
		int sequence;
		float timestamp;

		public static void Start() {
			time = Time.realtimeSinceStartup;
			Client.Listen<AckMessage>(m => clientMessages.Remove(m.sequence));
			Server.Listen<AckMessage>(m => serverMessages.Remove((m.connection, m.sequence)));
		}

		///<summary>Resend stale unacknowledged messages.</summary>
		public static void Update() {
			time = Time.realtimeSinceStartup;

			var clientSequences = clientMessages.Keys.ToList();
			clientSequences.Sort();
			foreach (var sequence in clientSequences) {
				var message = clientMessages[sequence];
				if (message.resends == 8) {
					Application.Quit();
					return;
				}
				if (time - message.timestamp > Delays[message.resends]) {
					message.Resend();
				}
			}

			var serverSequences = serverMessages.Keys.ToList();
			serverSequences.Sort();
			foreach (var client in serverSequences) {
				var message = serverMessages[client];
				if (message.resends == 8) {
					// TODO: disconnect client
					return;
				}
				if (time - message.timestamp > Delays[message.resends]) {
					message.Resend(client.connection);
				}
			}
		}

		///<summary>Send from client to server.</summary>
		public override void Send() {
			if (this is IClientMessage message) {
				timestamp = time;
				clientMessages[outgoingSequence] = this;
				using (var writer = new Writer()) {
					writer.Write((ushort)Types.IndexOf(this.GetType()));
					writer.Write(outgoingSequence);
					sequence = outgoingSequence;
					++outgoingSequence;
					message.Write(writer);
					var bytes = writer.ToArray();
					Client.Send(bytes);
				}
			}
		}

		///<summary>Send from server to client.</summary>
		public override void Send(int connection) {
			if (this is IServerMessage message) {
				timestamp = time;
				if (!outgoingSequences.ContainsKey(connection)) {
					outgoingSequences[connection] = 0;
				}
				serverMessages[(connection, outgoingSequences[connection])] = this;
				using (var writer = new Writer()) {
					writer.Write((ushort)Types.IndexOf(this.GetType()));
					writer.Write(outgoingSequences[connection]);
					sequence = outgoingSequences[connection];
					++outgoingSequences[connection];
					message.Write(writer);
					var bytes = writer.ToArray();
					Server.Send(bytes, connection);
				}
			}
		}

		///<summary>Broadcast from server to all clients.</summary>
		public override void Broadcast() {
			if (this is IServerMessage message) {
				var connections = GameObject.FindObjectOfType<Server>().Connections;
				for (var i = 0; i < connections.Length; ++i) {
					Send(connections[i].InternalId);
				}
			}
		}

		///<summary>Receive on server from client.</summary>
		internal override void Receive(Reader reader, int connection) {
			if (this is IClientMessage message) {
				reader.Read(out int sequence);
				new AckMessage(sequence).Send(connection);
				if (!incomingSequences.TryGetValue(connection, out var expectedSequence)) {
					incomingSequences[connection] = expectedSequence = 0;
				}
				if (sequence > expectedSequence) {
					message.Read(reader);
					waitingOnServer[(connection, sequence)] = this;
					return;
				} else if (sequence < expectedSequence) {
					// Already received and handled this message, disregard.
					return;
				}
				this.connection = connection;
				base.Receive(reader, connection);
				++incomingSequences[connection];
				while (waitingOnServer.TryGetValue((connection, incomingSequences[connection]), out var waitingMessage)) {
					waitingOnServer.Remove((connection, incomingSequences[connection]));
					Server.receivedMessages.Add(waitingMessage);
					++incomingSequences[connection];
				}
			}
		}

		///<summary>Receive on client from server.</summary>
		internal override void Receive(Reader reader) {
			if (this is IServerMessage message) {
				reader.Read(out int sequence);
				new AckMessage(sequence).Send();
				if (sequence > incomingSequence) {
					message.Read(reader);
					waitingOnClient[sequence] = this;
					return;
				} else if (sequence < incomingSequence) {
					// Already received and handled this message, disregard.
					return;
				}
				base.Receive(reader);
				++incomingSequence;
				while (waitingOnClient.TryGetValue(incomingSequence, out var waitingMessage)) {
					waitingOnClient.Remove(incomingSequence);
					Client.receivedMessages.Add(waitingMessage);
					++incomingSequence;
				}
			}
		}

		///<summary>Resend from client to server.</summary>
		void Resend() {
			if (this is IClientMessage message) {
				timestamp = time;
				++resends;
				using (var writer = new Writer()) {
					writer.Write((ushort)Types.IndexOf(this.GetType()));
					writer.Write(sequence);
					message.Write(writer);
					var bytes = writer.ToArray();
					Client.Send(bytes);
				}
			}
		}

		///<summary>Resend from server to client.</summary>
		void Resend(int connection) {
			if (this is IServerMessage message) {
				timestamp = time;
				++resends;
				using (var writer = new Writer()) {
					writer.Write((ushort)Types.IndexOf(this.GetType()));
					writer.Write(sequence);
					message.Write(writer);
					var bytes = writer.ToArray();
					Server.Send(bytes, connection);
				}
			}
		}

		class AckMessage : Message, IServerMessage, IClientMessage {
			public int sequence;

			protected override int length => sizeof(int);

			public AckMessage() { }
			public AckMessage(int sequence) {
				this.sequence = sequence;
			}

			public void Read(Reader reader) {
				reader.Read(out sequence);
			}

			public void Write(Writer writer) {
				writer.Write(sequence);
			}
		}
	}
}