namespace Net {
	using System.Reflection;
	using System.Runtime.InteropServices;

	///<summary>Automatically serializes and deserializes simple fields.</summary>
	public abstract class SimpleMessage : Message, IServerMessage, IClientMessage {
		FieldInfo[] fields => GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

		protected override int length {
			get {
				var size = 0;
				foreach (var field in fields) {
					var type = field.FieldType;
					if (type.IsValueType) {
						size += Marshal.SizeOf(type);
					} else if (type == typeof(string)) {
						var value = (string)field.GetValue(this);
						size += StringSize(value);
					}
				}
				return size;
			}
		}

		public void Read(Reader reader) {
			foreach (var field in fields) {
				var type = field.FieldType;
				if (type.IsValueType || type == typeof(string)) {
					reader.Read(type, out var value);
					field.SetValue(this, value);
				}
			}
		}

		public void Write(Writer writer) {
			foreach (var field in fields) {
				var type = field.FieldType;
				if (type.IsValueType || type == typeof(string)) {
					var value = field.GetValue(this);
					writer.Write(type, value);
				}
			}
		}
	}
}