namespace Net {
	using System;
	using System.Collections.Generic;
	using System.Text;
	using UnityEngine;

	public abstract class Message {
		//protected static List<Message> clientReceivedMessages = new List<Message>();
		//protected static List<Message> serverReceivedMessages = new List<Message>();
		static List<System.Type> types;

		internal static List<System.Type> Types =>
			types = types ?? Reflector.ImplementationsOf<Message>();
		protected static int StringSize(string text) =>
			sizeof(int) + Encoding.UTF8.GetByteCount(text);
		static Dictionary<System.Type, Action<Message>> onServerReceive = new Dictionary<Type, Action<Message>>();
		static Dictionary<System.Type, Action<Message>> onClientReceive = new Dictionary<Type, Action<Message>>();

		internal int connection;

		protected abstract int length { get; }

		///<summary>Send from client to server.</summary>
		public virtual void Send() {
			if (this is IClientMessage message) {
				using (var writer = new Writer()) {
					writer.Write((ushort)Types.IndexOf(this.GetType()));
					message.Write(writer);
					var bytes = writer.ToArray();
					Client.Send(bytes);
				}
			}
		}

		///<summary>Send from server to client.</summary>
		public virtual void Send(int connection) {
			if (this is IServerMessage message) {
				using (var writer = new Writer()) {
					writer.Write((ushort)Types.IndexOf(this.GetType()));
					message.Write(writer);
					var bytes = writer.ToArray();
					Server.Send(bytes, connection);
				}
			}
		}

		///<summary>Broadcast from server to all clients.</summary>
		public virtual void Broadcast() {
			if (this is IServerMessage message) {
				using (var writer = new Writer()) {
					writer.Write((ushort)Types.IndexOf(this.GetType()));
					message.Write(writer);
					var bytes = writer.ToArray();
					Server.Broadcast(bytes);
				}
			}
		}

		///<summary>Receive on server from client.</summary>
		internal virtual void Receive(Reader reader, int connection) {
			this.connection = connection;
			if (this is IClientMessage message) {
				message.Read(reader);
				Server.receivedMessages.Add(this);
				//serverReceivedMessages.Add(this);
			} else {
				Debug.LogWarning($"Server received illegal message {GetType()}, ignoring.");
			}
		}

		///<summary>Receive on client from server.</summary>
		internal virtual void Receive(Reader reader) {
			if (this is IServerMessage message) {
				message.Read(reader);
				Client.receivedMessages.Add(this);
				//clientReceivedMessages.Add(this);
			} else {
				Debug.LogWarning($"Client received illegal message {GetType()}, ignoring.");
			}
		}
	}
}