using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static SmartHomeTool.SmartHomeLibrary.Commands;

namespace InteligentnyDomRelay.SmartHomeLibrary
{
	public class Devices
	{
		public uint Address { get; set; }
		public CentralUnitDeviceItem.LineNumber LineNumber { get; set; }
		public DeviceVersion.HardwareType1Enum HardwareType1 { get; set; }
		public DeviceVersion.HardwareType2Enum HardwareType2 { get; set; }
		public byte HardwareSegmentsCount { get; set; }
		public byte HardwareVersion { get; set; }
		//public DbSet<Devices> ParentItem { get; set; }
		public bool Active { get; set; }
	}

	internal class DevicesCu
	{
		public uint Address { get; set; }
		public DateTime LastUpdated { get; set; }
		public bool Error { get; set; }
		public DateTime? ErrorFrom { get; set; }
		public uint Uptime { get; set; }
		public float Vin { get; set; }
	}

	internal class DevicesRelays
	{
		public uint Address { get; set; }
		public byte Segment { get; set; }
		public DateTime LastUpdated { get; set; }
		public bool Relay { get; set; }
		public bool Error { get; set; }
		public DateTime? ErrorFrom { get; set; }
		public uint Uptime { get; set; }
		public float Vin { get; set; }
	}

	internal class DevicesTemperatures
	{
		public uint Address { get; set; }
		public byte Segment { get; set; }
		public DateTime LastUpdated { get; set; }
		public float Temperature { get; set; }
		public bool Error { get; set; }
		public DateTime? ErrorFrom { get; set; }
		public uint Uptime { get; set; }
		public float Vin { get; set; }
	}
}
