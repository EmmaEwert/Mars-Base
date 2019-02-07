namespace Sandbox.Net {
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Net;
	using Unity.Collections;
	using Unity.Jobs;
	using Unity.Networking.Transport;
	using UnityEngine;

	public class Server : MonoBehaviour {
		public static Dictionary<int, string> players = new Dictionary<int, string>();
		public static NetworkConnection[] Connections = new NetworkConnection[0];
		internal static List<Message> receivedMessages = new List<Message>();
		static NativeList<NetworkConnection> connections;
		static BasicNetworkDriver<IPv4UDPSocket> driver;
		static ConcurrentQueue<NativeArray<byte>> broadcasts = new ConcurrentQueue<NativeArray<byte>>();
		static List<(int connection, NativeArray<byte> data)> messages = new List<(int, NativeArray<byte>)>();
		static JobHandle receiveJobHandle;
		static JobHandle[] sendJobHandles = new JobHandle[0];
		static JobHandle[] broadcastJobHandles = new JobHandle[0];
		static float ping;
		static Dictionary<System.Type, Action<Message>> handlers = new Dictionary<Type, Action<Message>>();

		public static void Listen<T>(Action<T> handler) where T : Message {
			if (Server.handlers.TryGetValue(typeof(T), out var handlers)) {
				Server.handlers[typeof(T)] = handlers + new Action<Message>(o => handler((T)o));
			} else {
				Server.handlers[typeof(T)] = new Action<Message>(o => handler((T)o));
			}
		}

		///<summary>Start a local server and a client with the given player name.</summary>
		void Start() {
			driver = new BasicNetworkDriver<IPv4UDPSocket>(new INetworkParameter[0]);
			var endpoint = new IPEndPoint(IP.local, 54889);
			if (driver.Bind(endpoint) != 0) {
				Debug.Log($"S: Failed to bind {endpoint.Address}:{endpoint.Port}");
			} else {
				driver.Listen();
				Debug.Log($"S: Listening on {endpoint.Address}:{endpoint.Port}…");
			}
			connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
			Listen<ConnectClientMessage>(SendPlayerState);
			Listen<PingMessage>(_ => {});
		}

		///<summary>Clean up server handles.</summary>
		public static void Stop() {
			receiveJobHandle.Complete();
			for (var i = 0; i < sendJobHandles.Length; ++i) {
				sendJobHandles[i].Complete();
				messages[i].data.Dispose();
			}
			messages.RemoveRange(0, sendJobHandles.Length);
			for (var i = 0; i < broadcastJobHandles.Length; ++i) {
				broadcastJobHandles[i].Complete();
				//broadcasts[i].Dispose();
			}
			//broadcasts.RemoveRange(0, broadcastJobHandles.Length);
			driver.Dispose();
			connections.Dispose();
		}

		///<summary>Update the server state through network IO.</summary>
		void Update() {
			// Finish up last frame's jobs
			receiveJobHandle.Complete();
			for (var i = 0; i < sendJobHandles.Length; ++i) {
				sendJobHandles[i].Complete();
				messages[i].data.Dispose();
			}
			messages.RemoveRange(0, sendJobHandles.Length);
			for (var i = 0; i < broadcastJobHandles.Length; ++i) {
				broadcastJobHandles[i].Complete();
			}
			Connections = connections.ToArray();

			// Process received messages
			var messageCount = receivedMessages.Count;
			for (var i = 0; i < messageCount; ++i) {
				OnReceive(receivedMessages[i]);
			}
			receivedMessages.RemoveRange(0, messageCount);


			// Schedule new network connection and event reception
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

			// Schedule message queue sending
			sendJobHandles = new JobHandle[messages.Count];
			for (var i = 0; i < messages.Count; ++i) {
				receiveJobHandle = new SendJob {
					driver = concurrentDriver,
					connections = connections,
					message = messages[i].data,
					index = messages[i].connection
				}.Schedule(receiveJobHandle);
			}

			// Schedule broadcast queue sending
			broadcastJobHandles = new JobHandle[broadcasts.Count];
			for (var i = 0; i < broadcastJobHandles.Length; ++i) {
				broadcasts.TryDequeue(out var message);
				receiveJobHandle = broadcastJobHandles[i] = new BroadcastJob {
					driver = concurrentDriver,
					connections = connections.AsDeferredJobArray(),
					message = message
				}.Schedule(connections, 1, receiveJobHandle);
			}
			//broadcasts.RemoveRange(0, broadcastJobHandles.Length);

			JobHandle.ScheduleBatchedJobs();

			// Send pings
			ping += Time.deltaTime;
			if (ping >= 5f) {
				ping = 0f;
				new PingMessage().Broadcast();
			}
		}

		internal static void Broadcast(byte[] bytes) {
			var data = new NativeArray<byte>(bytes, Allocator.TempJob);
			broadcasts.Enqueue(data);
		}

		internal static void Send(byte[] bytes, int connection) {
			var data = new NativeArray<byte>(bytes, Allocator.TempJob);
			messages.Add((connection, data));
			//list.Add(data);
		}

		static void SendPlayerState(ConnectClientMessage message) {
			players.Add(message.connection, message.name);
			new ConnectServerMessage(message.connection).Send(message.connection);
			new JoinMessage(message.connection, message.name).Broadcast();
			foreach (var player in Server.players) {
				if (player.Key != message.connection) {
					new JoinMessage(player.Key, player.Value).Send(message.connection);
				}
			}
		}

		static void OnReceive(Message message) {
			if (handlers.TryGetValue(message.GetType(), out var handler)) {
				handler(message);
			} else {
				Debug.Log($"No server handlers for {message.GetType()}, ignoring…");
			}
		}
		
		static void Receive(Reader reader, int connection) {
			reader.Read(out ushort typeIndex);
			var type = Message.Types[typeIndex];
			var message = (Message)Activator.CreateInstance(type);
			message.Receive(reader, connection);
		}

		///<summary>Clean up network internals before quitting.</summary>
		void OnApplicationQuit() {
			Server.Stop();
		}

		struct UpdateJob : IJob {
			public BasicNetworkDriver<IPv4UDPSocket> driver;
			public NativeList<NetworkConnection> connections;

			public void Execute() {
				// Clean up connections
				for (var i = 0; i < connections.Length; ++i) {
					if (!connections[i].IsCreated) {
						connections.RemoveAtSwapBack(i);
						--i;
					}
				}

				// Accept new connections
				NetworkConnection c;
				while ((c = driver.Accept()) != default(NetworkConnection)) {
					Debug.Log($"S: Accepted connection from {c.InternalId}");
					connections.Add(c);
				}
			}
		}

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
								Receive(reader, connections[index].InternalId);
							}
							break;
					}
				}
			}
		}

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