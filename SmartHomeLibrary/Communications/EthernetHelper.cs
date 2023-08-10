using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
	public class NetworkCardItem
	{
		public bool up;
		public string? name;
		public string? description;
		public IPAddress? ip;
		public IPAddress? mask;
		public IPAddress? gateway;
	}

	public class EthernetHelper
	{
		public static List<NetworkCardItem> GetNetworkInterfaces()
		{
			List<NetworkCardItem> items = new();
			foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
					continue;
				
				IPInterfaceProperties ipip = nic.GetIPProperties();
				NetworkCardItem item = new()
				{
					up = nic.OperationalStatus == OperationalStatus.Up,
					name = nic.Name,
					description = nic.Description
				};
				foreach (UnicastIPAddressInformation ip in ipip.UnicastAddresses)
					if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
					{
						item.ip = ip.Address;
						item.mask = ip.IPv4Mask;
					}
				foreach (GatewayIPAddressInformation ip in ipip.GatewayAddresses)
					if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
						item.gateway = ip.Address;
				if (item.ip != null)
					items.Add(item);
			}
			return items;
		}

		public static long IPAddressToLong(IPAddress ip)
		{
			byte[] bytes = ip.GetAddressBytes();
			return ((long)bytes[0] << 24) | ((long)bytes[1] << 16) | ((long)bytes[2] << 8) | (long)bytes[3];
		}

		public static IPAddress IPAddressFromLong(long a)
		{
			byte[] bytes = new byte[] { (byte)((a >> 24) & 0xff), (byte)((a >> 16) & 0xff),
					(byte)((a >> 8) & 0xff), (byte)(a & 0xff) };
			return new IPAddress(bytes);
		}

		public static void Scan(IPAddress ip, IPAddress mask, EventHandler onComplete)
		{
			List<IPAddress> output = new();
			int replays = 0;
			long ipfrom = IPAddressToLong(ip) & IPAddressToLong(mask);
			long ipto = ipfrom + (IPAddressToLong(mask) ^ 0xffffffffL) - 1;
			ipfrom++;
			for (long i = ipfrom; i <= ipto; i++)
			{
				Ping ping = new();
				ping.PingCompleted += (object sender, PingCompletedEventArgs e) =>
				{
					Ping ping_ = (Ping)sender;
					IPAddress ip = IPAddressFromLong((long)e.UserState);
					if (e.Reply.Status == IPStatus.Success)
						output.Add(ip);
					if (++replays == ipto - ipfrom + 1)
						onComplete?.Invoke(output, null);
				};
				ping.SendAsync(IPAddressFromLong(i), 500, i);
			}

			// string hostname, ushort port
			//TcpClient tcpClient = new TcpClient(hostname, port);
		}
	}
}
