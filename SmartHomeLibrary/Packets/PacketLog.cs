using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
	public class PacketLog
	{
		public enum Type { Packet, Debug }

		public Type type;
		public DateTime dt = DateTime.Now;
		public byte[] data;
		public Packets.PacketDirection packetDirection;
		public string text;
		public bool isError;

		public PacketLog(Type type, DateTime dt, byte[] data, Packets.PacketDirection packetDirection,
				string text = "", bool isError = false)
		{
			this.type = type;
			this.dt = dt;
			this.data = data;
			this.packetDirection = packetDirection;
			this.text = text;
			this.isError = isError;
		}
	}
}
