namespace Sandbox.Net {
	using System;
	using System.Collections.Generic;
	using System.Net;
	using Unity.Collections;
	using Unity.Jobs;
	using Unity.Networking.Transport;
	using UnityEngine;

	public class Client : MonoBehaviour {
		internal static Dictionary<int, string> players = new Dictionary<int, string>();
		internal static int id;
		internal static List<Message> receivedMessages = new List<Message>();
		static BasicNetworkDriver<IPv4UDPSocket> driver;
		static NativeArray<NetworkConnection> connection;
		static List<NativeArray<byte>> messages = new List<NativeArray<byte>>();
		static JobHandle receiveJobHandle;
		static JobHandle[] sendJobHandles = new JobHandle[0];
		static Dictionary<System.Type, Action<Message>> handlers = new Dictionary<Type, Action<Message>>();
		internal string remoteIP;
		public static string playerName;

		public static void Listen<T>(Action<T> handler) where T : Message {
			if (Client.handlers.TryGetValue(typeof(T), out var handlers)) {
				Client.handlers[typeof(T)] = handlers + new Action<Message>(o => handler((T)o));
			} else {
				Client.handlers[typeof(T)] = new Action<Message>(o => handler((T)o));
			}
		}

		void Start() {
			driver = new BasicNetworkDriver<IPv4UDPSocket>(new INetworkParameter[0]);
			connection = new NativeArray<NetworkConnection>(1, Allocator.Persistent);
			var endpoint = new IPEndPoint(IPAddress.Parse(remoteIP), 54889);
			connection[0] = driver.Connect(endpoint);
			Debug.Log($"C: Connecting to {endpoint}…");
			Listen<ConnectServerMessage>(message => id = message.id);
			Listen<JoinMessage>(message => players[message.id] = message.name);
			Listen<PingMessage>(message => message.Send());
			ReliableMessage.Start();
		}

		public static void Stop() {
			receiveJobHandle.Complete();
			for (var i = 0; i < sendJobHandles.Length; ++i) {
				sendJobHandles[i].Complete();
			}
			for (var i = 0; i < messages.Count; ++i) {
				messages[i].Dispose();
			}
			connection.Dispose();
			driver.Dispose();
			// TODO: Disconnect if connected
		}

		void Update() {
			// Finish up last frame's jobs
			receiveJobHandle.Complete();
			for (var i = 0; i < sendJobHandles.Length; ++i) {
				sendJobHandles[i].Complete();
				messages[i].Dispose();
			}
			messages.RemoveRange(0, sendJobHandles.Length);

			// Process received messages
			var messageCount = receivedMessages.Count;
			for (var i = 0; i < messageCount; ++i) {
				OnReceive(receivedMessages[i]);
			}
			receivedMessages.RemoveRange(0, messageCount);

			// Schedule new network event reception
			var receiveJob = new ReceiveJob {
				driver = driver,
				connection = connection,
			};
			receiveJobHandle = driver.ScheduleUpdate();
			receiveJobHandle = receiveJob.Schedule(receiveJobHandle);

			// Schedule message queue sending
			sendJobHandles = new JobHandle[messages.Count];
			for (var i = 0; i < messages.Count; ++i) {
				sendJobHandles[i] = receiveJobHandle = new SendJob {
					driver = driver,
					connection = connection,
					message = messages[i]
				}.Schedule(receiveJobHandle);
			}

			ReliableMessage.Update();
		}

		static void OnReceive(Message message) {
			if (handlers.TryGetValue(message.GetType(), out var handler)) {
				handler(message);
			} else {
				Debug.Log($"No client handlers for {message.GetType()}, ignoring…");
			}
		}

		internal static void Send(byte[] bytes) {
			var data = new NativeArray<byte>(bytes, Allocator.TempJob);
			messages.Add(data);
		}

		static void Receive(Reader reader) {
			reader.Read(out ushort typeIndex);
			var type = Message.Types[typeIndex];
			var message = (Message)Activator.CreateInstance(type);
			message.Receive(reader);
		}

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
								Receive(reader);
							}
							break;
					}
				}
			}
		}

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