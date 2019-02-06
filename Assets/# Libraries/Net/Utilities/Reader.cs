namespace Sandbox.Net {
	using System;
	using System.IO;
	using System.IO.Compression;
	using System.Text;
	using Unity.Mathematics;
	using Unity.Networking.Transport;
	using static Unity.Mathematics.math;
	using static Unity.Networking.Transport.DataStreamReader;

	public class Reader : IDisposable {
		BinaryReader reader;
		MemoryStream stream;

		internal Reader(DataStreamReader reader) {
			var context = default(Context);
			var length = reader.ReadInt(ref context);
			var bytes = reader.ReadBytesAsArray(ref context, length);
			stream = new MemoryStream();
			using (var inStream = new MemoryStream(bytes)) {
				using (var deflateStream = new DeflateStream(inStream, CompressionMode.Decompress)) {
					deflateStream.CopyTo(stream);
				}
			}
			stream.Seek(0, SeekOrigin.Begin);
			this.reader = new BinaryReader(stream);
		}

		public void Dispose() {
			reader.Dispose();
			stream.Dispose();
		}

		internal void Read(ref ushort[] value) {
			var bytes = reader.ReadBytes(sizeof(ushort) * value.Length);
			Buffer.BlockCopy(bytes, 0, value, 0, bytes.Length);
		}

		internal void Read(out ushort value) {
			value = reader.ReadUInt16();
		}

		internal void Read(out int value) {
			value = reader.ReadInt32();
		}

		internal void Read(out string value) {
			var length = reader.ReadInt32();
			var bytes = reader.ReadBytes(length);
			value = Encoding.UTF8.GetString(bytes);
		}

		internal void Read(out int3 value) {
			value = int3(
				reader.ReadInt32(),
				reader.ReadInt32(),
				reader.ReadInt32()
			);
		}

		internal void Read(out float3 value) {
			value = float3(
				reader.ReadSingle(),
				reader.ReadSingle(),
				reader.ReadSingle()
			);
		}

		internal void Read(out quaternion value) {
			value = quaternion(
				reader.ReadSingle(),
				reader.ReadSingle(),
				reader.ReadSingle(),
				reader.ReadSingle()
			);
		}

	}
}