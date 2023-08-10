using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using static SmartHomeTool.SmartHomeLibrary.Commands;

namespace SmartHomeTool.SmartHomeLibrary
{
	class UpgradeProgram
	{
		public const int BootloaderPacketLength = 256;
		public const int PacketDelay = 40;

		//public bool isHubLine = false;

		public byte[] loadedProgramData = Array.Empty<byte>();
		public string loadedProgramPath = "";
		public DeviceClass loadedProgramDeviceClass = new();
		public DateTime modifyDateTime;
		public DateTime versionDate;
		public DeviceVersion? deviceVersion;

		public ushort GetPacketsCount()
		{
			return (ushort)Math.Ceiling((float)loadedProgramData.Length / BootloaderPacketLength);
		}

		public byte[] GetPacket(ushort packetNumber)
		{
			int length = BootloaderPacketLength;
			ushort packetsCount = GetPacketsCount();
			if (packetNumber == packetsCount - 1)
				length = loadedProgramData.Length - packetNumber * BootloaderPacketLength;
			byte[] packet = new byte[length];
			Array.Copy(loadedProgramData, packetNumber * BootloaderPacketLength, packet, 0, length);
			return packet;
		}

		static bool CheckProgramString(byte[] data, string s, out byte major, out byte minor)
		{
			major = 0;
			minor = 0;
			int i = Common.IsSubArray(data, Encoding.ASCII.GetBytes(s));
			if (i > 0)
			{
				major = data[i + s.Length];
				minor = data[i + s.Length + 1];
			}
			return i > 0;
		}

		public static DeviceVersion? DecodeProgramFromBinary(byte[] programData)
		{
			DeviceVersion deviceVersion = new();
			List<byte> bytesList = new(new byte[] { 0x57, 0x9a, 0x93, 0xed });
			bytesList.AddRange(Encoding.ASCII.GetBytes("SmartHome Program "));
			int i = Common.IsSubArray(programData, bytesList.ToArray());
			if (i < 0)
				return null;

			i += bytesList.Count;
			int i2 = Common.IsSubArray(programData, new byte[] { (byte)' ' }, i);
			if (i2 < 0)
				return null;

			string type1 = Encoding.ASCII.GetString(programData[i..i2]);
			switch (type1)
			{
				case "Din":      deviceVersion.HardwareType1 = DeviceVersion.HardwareType1Enum.DIN; break;
				case "Box":      deviceVersion.HardwareType1 = DeviceVersion.HardwareType1Enum.BOX; break;
				case "RadioBox": deviceVersion.HardwareType1 = DeviceVersion.HardwareType1Enum.RadioBOX; break;
				case "Common":   deviceVersion.HardwareType1 = DeviceVersion.HardwareType1Enum.Common; break;
				default: return null;
			}

			i += type1.Length + 1;
			i2 = Common.IsSubArray(programData, new byte[] { (byte)' ' }, i);
			if (i2 < 0)
				return null;

			string type2 = Encoding.ASCII.GetString(programData[i..i2]);
			switch (type2)
			{
				case "CU":         deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.CU; break;
				case "CU_WR":      deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.CU_WR; break;
				case "Expander":   deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Expander; break;
				case "Radio":      deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Radio; break;
				case "Amplifier":  deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Amplifier; break;
				case "Acin":       deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Acin; break;
				case "Anin":       deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Anin; break;
				case "Anout":      deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Anout; break;
				case "Digin":      deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Digin; break;
				case "Dim":        deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Dim; break;
				case "Led":        deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Led; break;
				case "Mul":        deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Mul; break;
				case "Rel":        deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Rel; break;
				case "Rol":        deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Rol; break;
				case "Temp":       deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Temp; break;
				case "Tablet":     deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.Tablet; break;
				case "TouchPanel": deviceVersion.HardwareType2 = DeviceVersion.HardwareType2Enum.TouchPanel; break;
				default:           return null;
			}

			i += type2.Length + 1;
			if (i + 10 >= programData.Length)
				return null;

			if (!Enum.IsDefined(typeof(DeviceVersion.HardwareType1Enum), (DeviceVersion.HardwareType1Enum)programData[i]))
				return null;
			DeviceVersion.HardwareType1Enum type1Enum = (DeviceVersion.HardwareType1Enum)programData[i++];

			if (!Enum.IsDefined(typeof(DeviceVersion.HardwareType2Enum), (DeviceVersion.HardwareType2Enum)programData[i]))
				return null;
			DeviceVersion.HardwareType2Enum type2Enum = (DeviceVersion.HardwareType2Enum)programData[i++];

			if (deviceVersion.HardwareType1 != type1Enum || deviceVersion.HardwareType2 != type2Enum)
				return null;

			deviceVersion.HardwareSegmentsCount = programData[i++];

			deviceVersion.ProgramVersionMajor = programData[i++];
			deviceVersion.ProgramVersionMinor = programData[i++];
			
			try
			{
				deviceVersion.ProgramDateTime = new DateTime(2000 + programData[i++], programData[i++], programData[i++]);
			}
			catch
			{
				return null;
			}

			return deviceVersion;
		}

		public string VersionToString()
		{
			return $"Program loaded - '{Path.GetFileName(loadedProgramPath):s}' - " +
					$"{deviceVersion.HardwareType1.ToString().ToUpper()}-" +
					$"{deviceVersion.HardwareType2.ToString().ToUpper()}-" +
					$"{deviceVersion.HardwareSegmentsCount} " +
					$"v{deviceVersion.ProgramVersionMajor}.{deviceVersion.ProgramVersionMinor} - " +
					$"{deviceVersion.ProgramDateTime.ToShortDateString()} - " +
					$"{loadedProgramData.Length} bytes - " +
					$"{GetPacketsCount()} packets - " +
					$"CRC32 0x{Crc32.CalculateCrc32(0, loadedProgramData):x8}";
		}

		//public int HowManyProgramsInside()
		//{
		//  const string Separator = "[DisplaySeparator]";
		//  int founds = 0;
		//  byte[] bytes = ASCIIEncoding.ASCII.GetBytes(Separator);
		//  for (int i = 0; i < loadedProgramData.Length - bytes.Length; i++)
		//  {
		//    bool ok = true;
		//    for (int j = 0; j < Separator.Length; j++)
		//      if (loadedProgramData[i + j] != bytes[j])
		//      {
		//        ok = false;
		//        break;
		//      }
		//    if (ok)
		//      founds++;
		//  }
		//  return founds;
		//}
	}
}
