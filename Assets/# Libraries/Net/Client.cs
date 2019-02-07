namespace Sandbox.Net {
	using System;
	using System.Collections.Generic;
	using System.Net;
	using Unity.Collections;
	using Unity.Jobs;
	using Unity.Networking.Transport;
	using UnityEngine;

	public class Client : MonoBehaviour {
		public static string playerName;
		internal static List<Message> receivedMessages = new List<Message>();
		static List<NativeArray<byte>> sentMessages = new List<NativeArray<byte>>();
		static Dictionary<Type, Action<Message>> handlers = new Dictionary<Type, Action<Message>>();

		///<summary>Add a handler to be called when client receives a Message of a specific type.</summary>
		public static void Listen<T>(Action<T> handler) where T : Message {
			if (Client.handlers.TryGetValue(typeof(T), out var handlers)) {
				Client.handlers[typeof(T)] = handlers + new Action<Message>(o => handler((T)o));
			} else {
				Client.handlers[typeof(T)] = new Action<Message>(o => handler((T)o));
			}
		}

		///<summary>Queue a Message-as-byte-array for sending to the server.</summary>
		internal static void Send(byte[] bytes) {
			var data = new NativeArray<byte>(bytes, Allocator.TempJob);
			sentMessages.Add(data);
		}

		public string remoteIP;
		///<summary>Client's unique connection ID on the server.</summary>
		internal int connectionID;
		///<summary>Other clients' names and unique connection IDs on the server.</summary>
		internal Dictionary<int, string> playerConnections = new Dictionary<int, string>();
		BasicNetworkDriver<IPv4UDPSocket> driver;
		NativeArray<NetworkConnection> connection;
		JobHandle receiveJobHandle;
		JobHandle[] sendJobHandles = new JobHandle[0];

		///<summary>Call all handlers listening for a Message, based on its type.</summary>
		void Handle(Message message) {
			if (handlers.TryGetValue(message.GetType(), out var handler)) {
				handler(message);
			} else {
				Debug.Log($"C: No handlers for {message.GetType()}, ignoring…");
			}
		}

		///<summary>Set up listeners and data structures, and connect to server.</summary>
		void Start() {
			Listen<ConnectServerMessage>(message => connectionID = message.id);
			Listen<JoinMessage>(message => playerConnections[message.id] = message.name);
			Listen<PingMessage>(message => message.Send());
			driver = new BasicNetworkDriver<IPv4UDPSocket>(new INetworkParameter[0]);
			connection = new NativeArray<NetworkConnection>(1, Allocator.Persistent);
			var endpoint = new IPEndPoint(IPAddress.Parse(remoteIP), 54889);
			connection[0] = driver.Connect(endpoint);
			Debug.Log($"C: Connecting to {endpoint}…");
			ReliableMessage.Start();
		}

		///<summary>Handle messages, and update message and network state.</summary>
		void Update() {
			// Finish up last frame's jobs.
			receiveJobHandle.Complete();
			for (var i = 0; i < sendJobHandles.Length; ++i) {
				sendJobHandles[i].Complete();
				sentMessages[i].Dispose();
			}
			sentMessages.RemoveRange(0, sendJobHandles.Length);

			// Process received messages on the main thread.
			var messageCount = receivedMessages.Count;
			for (var i = 0; i < messageCount; ++i) {
				Handle(receivedMessages[i]);
			}
			receivedMessages.RemoveRange(0, messageCount);

			// Schedule new network event reception job.
			var receiveJob = new ReceiveJob {
				driver = driver,
				connection = connection,
			};
			receiveJobHandle = driver.ScheduleUpdate();
			receiveJobHandle = receiveJob.Schedule(receiveJobHandle);

			// Schedule message queue sending jobs.
			sendJobHandles = new JobHandle[sentMessages.Count];
			for (var i = 0; i < sentMessages.Count; ++i) {
				sendJobHandles[i] = receiveJobHandle = new SendJob {
					driver = driver,
					connection = connection,
					message = sentMessages[i]
				}.Schedule(receiveJobHandle);
			}

			ReliableMessage.Update();
		}

		///<summary>Clean up network internals before quitting.</summary>
		void OnApplicationQuit() {
			receiveJobHandle.Complete();
			for (var i = 0; i < sendJobHandles.Length; ++i) {
				sendJobHandles[i].Complete();
			}
			for (var i = 0; i < sentMessages.Count; ++i) {
				sentMessages[i].Dispose();
			}
			connection.Dispose();
			driver.Dispose();
			// TODO: Disconnect if connected
		}

		///<summary>Handles reception of all network events.</summary>
		struct ReceiveJob : IJob {
			public BasicNetworkDriver<IPv4UDPSocket> driver;
			public NativeArray<NetworkConnection> connection;

			public void Execute() {
				if (!connection[0].IsCreated) { return; }

				NetworkEvent.Type command;
				while ((command = connection[0].PopEvent(driver, out var streamReader)) != NetworkEvent.Type.Empty) {
					switch (command) {
						case NetworkEvent.Type.Connect:
							Debug.Log("C: Connected to server");
							new ConnectClientMessage(playerName).Send();
							break;
						case NetworkEvent.Type.Disconnect:
							Debug.Log("C: Disconnected from server");
							connection[0] = default(NetworkConnection);
							break;
						case NetworkEvent.Type.Data:
							using (var reader = new Reader(streamReader)) {
								reader.Read(out ushort typeIndex);
								var type = Message.Types[typeIndex];
								var message = (Message)Activator.CreateInstance(type);
								message.Receive(reader);
							}
							break;
					}
				}
			}
		}

		///<summary>Send queued messages to server.</summary>
		struct SendJob : IJob {
			public BasicNetworkDriver<IPv4UDPSocket> driver;
			[ReadOnly] public NativeArray<NetworkConnection> connection;
			[ReadOnly] public NativeArray<byte> message;

			public void Execute() {
				using (var writer = new DataStreamWriter(message.Length, Allocator.Temp)) {
					writer.Write(message.ToArray());
					driver.Send(connection[0], writer);
				}
			}
		}
	}
}