using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SmartHomeTool.SmartHomeLibrary.Commands.CentralUnitDeviceItem;
using static SmartHomeTool.SmartHomeLibrary.Commands.DeviceVersion;

namespace InteligentnyDomSimulator.SmartHomeLibrary
{
	class DeviceItem
	{
		public uint address;
		public LineNumber lineNumber;
		public HardwareType1Enum hardwareType1;
		public HardwareType2Enum hardwareType2;
		public byte hardwareSegmentsCount;
		public byte hardwareVersion;
		public DeviceItemStatus? status;
	}

	class DeviceItemStatus
	{
		public ushort voltage = 3300;
		public uint uptime = 1234;
		public bool error;
	}

	class TemperatureStatus : DeviceItemStatus
	{
		public ushort[] temperatures = Array.Empty<ushort>();
	}

	class RelayStatus : DeviceItemStatus
	{
		public bool[] relays = Array.Empty<bool>();
	}

	class DeviceItemHandler
	{
		public DeviceItemStatus status;
		public int index;
	}
}
