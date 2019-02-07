namespace Net {
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Net;
	using Unity.Collections;
	using Unity.Jobs;
	using Unity.Networking.Transport;
	using UnityEngine;

	public class Server : MonoBehaviour {
		internal static List<Message> receivedMessages = new List<Message>();
		static List<(int connection, NativeArray<byte> data)> sentMessages = new List<(int, NativeArray<byte>)>();
		static ConcurrentQueue<NativeArray<byte>> broadcasts = new ConcurrentQueue<NativeArray<byte>>();
		static Dictionary<System.Type, Action<Message>> handlers = new Dictionary<Type, Action<Message>>();

		///<summary>Add a handler to be called when server receives a Message of a specific type.</summary>
		public static void Listen<T>(Action<T> handler) where T : Message {
			if (Server.handlers.TryGetValue(typeof(T), out var handlers)) {
				Server.handlers[typeof(T)] = handlers + new Action<Message>(o => handler((T)o));
			} else {
				Server.handlers[typeof(T)] = new Action<Message>(o => handler((T)o));
			}
		}

		///<summary>Queue a Message-as-byte-array for sending to all clients.</summary>
		internal static void Broadcast(byte[] bytes) {
			var data = new NativeArray<byte>(bytes, Allocator.TempJob);
			broadcasts.Enqueue(data);
		}

		///<summary>Queue a Message-as-byte-array for sending to a client.</summary>
		internal static void Send(byte[] bytes, int connection) {
			var data = new NativeArray<byte>(bytes, Allocator.TempJob);
			sentMessages.Add((connection, data));
		}

		///<summary>Clients' names and unique connection IDs.</summary>
		public Dictionary<int, string> playerConnections = new Dictionary<int, string>();
		public NetworkConnection[] Connections = new NetworkConnection[0];
		NativeList<NetworkConnection> connections;
		BasicNetworkDriver<IPv4UDPSocket> driver;
		JobHandle receiveJobHandle;
		JobHandle[] sendJobHandles = new JobHandle[0];
		JobHandle[] broadcastJobHandles = new JobHandle[0];
		float ping;

		///<summary>Call all handlers listening for a Message, based on its type.</summary>
		void Handle(Message message) {
			if (handlers.TryGetValue(message.GetType(), out var handler)) {
				handler(message);
			} else {
				Debug.Log($"S: No handlers for {message.GetType()}, ignoring…");
			}
		}

		///<summary>Send a list of connected players to a newly connected player.</summary>
		void SendConnectedPlayers(ConnectClientMessage message) {
			playerConnections.Add(message.connection, message.name);
			new ConnectServerMessage(message.connection).Send(message.connection);
			new JoinMessage(message.connection, message.name).Broadcast();
			foreach (var player in playerConnections) {
				if (player.Key != message.connection) {
					new JoinMessage(player.Key, player.Value).Send(message.connection);
				}
			}
		}

		///<summary>Set up listeners and data structures, and listen for connections.</summary>
		void Start() {
			Listen<ConnectClientMessage>(SendConnectedPlayers);
			Listen<PingMessage>(_ => {});
			driver = new BasicNetworkDriver<IPv4UDPSocket>(new INetworkParameter[0]);
			var endpoint = new IPEndPoint(IP.local, 54889);
			if (driver.Bind(endpoint) != 0) {
				Debug.Log($"S: Failed to bind {endpoint.Address}:{endpoint.Port}");
			} else {
				driver.Listen();
				Debug.Log($"S: Listening on {endpoint.Address}:{endpoint.Port}…");
			}
			connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
		}

		///<summary>Handle messages, and update message and network state.</summary>
		void Update() {
			// Finish up last frame's jobs.
			receiveJobHandle.Complete();
			for (var i = 0; i < sendJobHandles.Length; ++i) {
				sendJobHandles[i].Complete();
				sentMessages[i].data.Dispose();
			}
			sentMessages.RemoveRange(0, sendJobHandles.Length);
			for (var i = 0; i < broadcastJobHandles.Length; ++i) {
				broadcastJobHandles[i].Complete();
			}
			Connections = connections.ToArray();

			// Process received messages.
			var messageCount = receivedMessages.Count;
			for (var i = 0; i < messageCount; ++i) {
				Handle(receivedMessages[i]);
			}
			receivedMessages.RemoveRange(0, messageCount);


			// Schedule new network connection and event reception.
			var concurrentDriver = driver.ToConcurrent();
			var updateJob = new UpdateJob {
				driver = driver,
				connections = connections
			};
			var receiveJob = new ReceiveJob {
				driver = concurrentDriver,
				connections = connections.AsDeferredJobArray()
			};
			receiveJobHandle = driver.ScheduleUpdate();
			receiveJobHandle = updateJob.Schedule(receiveJobHandle);
			receiveJobHandle = receiveJob.Schedule(connections, 1, receiveJobHandle);

			// Schedule message queue sending.
			sendJobHandles = new JobHandle[sentMessages.Count];
			for (var i = 0; i < sentMessages.Count; ++i) {
				receiveJobHandle = new SendJob {
					driver = concurrentDriver,
					connections = connections,
					message = sentMessages[i].data,
					index = sentMessages[i].connection
				}.Schedule(receiveJobHandle);
			}

			// Schedule broadcast queue sending.
			broadcastJobHandles = new JobHandle[broadcasts.Count];
			for (var i = 0; i < broadcastJobHandles.Length; ++i) {
				broadcasts.TryDequeue(out var message);
				receiveJobHandle = broadcastJobHandles[i] = new BroadcastJob {
					driver = concurrentDriver,
					connections = connections.AsDeferredJobArray(),
					message = message
				}.Schedule(connections, 1, receiveJobHandle);
			}

			JobHandle.ScheduleBatchedJobs();

			// HACK: Send pings to keep connections alive.
			ping += Time.deltaTime;
			if (ping >= 5f) {
				ping = 0f;
				new PingMessage().Broadcast();
			}
		}

		///<summary>Clean up network internals before quitting.</summary>
		void OnApplicationQuit() {
			receiveJobHandle.Complete();
			for (var i = 0; i < sendJobHandles.Length; ++i) {
				sendJobHandles[i].Complete();
				sentMessages[i].data.Dispose();
			}
			sentMessages.RemoveRange(0, sendJobHandles.Length);
			for (var i = 0; i < broadcastJobHandles.Length; ++i) {
				broadcastJobHandles[i].Complete();
			}
			driver.Dispose();
			connections.Dispose();
		}

		struct UpdateJob : IJob {
			public BasicNetworkDriver<IPv4UDPSocket> driver;
			public NativeList<NetworkConnection> connections;

			public void Execute() {
				// Clean up stale connections.
				for (var i = 0; i < connections.Length; ++i) {
					if (!connections[i].IsCreated) {
						connections.RemoveAtSwapBack(i);
						--i;
					}
				}
				// Accept new connections.
				NetworkConnection c;
				while ((c = driver.Accept()) != default(NetworkConnection)) {
					Debug.Log($"S: Accepted connection from {c.InternalId}");
					connections.Add(c);
				}
			}
		}

		///<summary>Handles reception of all network events.</summary>
		struct ReceiveJob : IJobParallelFor {
			public BasicNetworkDriver<IPv4UDPSocket>.Concurrent driver;
			public NativeArray<NetworkConnection> connections;

			public void Execute(int index) {
				NetworkEvent.Type command;
				while ((command = driver.PopEventForConnection(connections[index], out var streamReader)) != NetworkEvent.Type.Empty) {
					switch (command) {
						case NetworkEvent.Type.Disconnect:
							Debug.Log($"S: Client disconnected");
							connections[index] = default(NetworkConnection);
							break;
						case NetworkEvent.Type.Data:
							using (var reader = new Reader(streamReader)) {
								reader.Read(out ushort typeIndex);
								var type = Message.Types[typeIndex];
								var message = (Message)Activator.CreateInstance(type);
								message.Receive(reader, connections[index].InternalId);
							}
							break;
					}
				}
			}
		}

		///<summary>Send queued messages to clients.</summary>
		struct SendJob : IJob {
			public BasicNetworkDriver<IPv4UDPSocket>.Concurrent driver;
			[ReadOnly] public NativeList<NetworkConnection> connections;
			[ReadOnly] public NativeArray<byte> message;
			[ReadOnly] public int index;

			public void Execute() {
				using (var writer = new DataStreamWriter(message.Length, Allocator.Temp)) {
					writer.Write(message.ToArray());
					driver.Send(connections[index], writer);
				}
			}
		}

		///<summary>Send queued broadcasts to all clients.</summary>
		struct BroadcastJob : IJobParallelFor {
			public BasicNetworkDriver<IPv4UDPSocket>.Concurrent driver;
			[ReadOnly] public NativeArray<NetworkConnection> connections;
			[ReadOnly, DeallocateOnJobCompletion] public NativeArray<byte> message;

			public void Execute(int index) {
				using (var writer = new DataStreamWriter(message.Length, Allocator.Temp)) {
					writer.Write(message.ToArray());
					driver.Send(connections[index], writer);
				}
			}
		}
	}
}