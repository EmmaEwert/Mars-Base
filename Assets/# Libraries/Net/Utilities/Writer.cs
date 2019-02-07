namespace Net {
	using System;
	using System.IO;
	using System.IO.Compression;
	using System.Text;
	using Unity.Mathematics;

	public class Writer : IDisposable {
		private MemoryStream stream;
		private BinaryWriter writer;

		internal Writer() {
			stream = new MemoryStream();
			writer = new BinaryWriter(stream);
		}

		public void Dispose() {
			stream.Dispose();
			writer.Dispose();
		}

		internal byte[] ToArray() {
			var data = stream.ToArray();
			using (var outStream = new MemoryStream()) {
				using (var deflateStream = new DeflateStream(outStream, CompressionLevel.Optimal)) {
					deflateStream.Write(data, 0, data.Length);
				}
				data = outStream.ToArray();
			}
			var bytes = new byte[sizeof(int) + data.Length];
			Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, bytes, 0, sizeof(int));
			Buffer.BlockCopy(data, 0, bytes, sizeof(int), data.Length);
			return bytes;
		}

		internal void Write(Type type, object value) {
			if (type == typeof(int)) {
				Write((int)value);
			} else if (type == typeof(float3)) {
				Write((float3)value);
			} else if (type == typeof(string)) {
				Write((string)value);
			}
		}

		internal void Write(ushort[] value) {
			var bytes = new byte[value.Length * sizeof(ushort)];
			Buffer.BlockCopy(value, 0, bytes, 0, bytes.Length);
			writer.Write(bytes);
		}

		internal void Write(int value) => writer.Write(value);
		internal void Write(ushort value) => writer.Write(value);
		internal void Write(float value) => writer.Write(value);
		internal void Write(string value) {
			var bytes = Encoding.UTF8.GetBytes(value);
			writer.Write(bytes.Length);
			writer.Write(bytes);
		}
		internal void Write(int3 value) {
			Write(value.x);
			Write(value.y);
			Write(value.z);
		}
		internal void Write(float3 value) {
			Write(value.x);
			Write(value.y);
			Write(value.z);
		}
		internal void Write(quaternion value) {
			Write(value.value.x);
			Write(value.value.y);
			Write(value.value.z);
			Write(value.value.w);
		}
	}
}