namespace Net {
	using System.Net;
	using System.Net.Sockets;

	public static class IP {
		public static IPAddress local {
			get {
				var host = Dns.GetHostEntry(Dns.GetHostName());
				foreach (var ip in host.AddressList) {
					if (ip.AddressFamily == AddressFamily.InterNetwork) {
						return ip;
					}
				}
				return IPAddress.Loopback;
			}
		}
	}
}