using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SmartHomeTool.SmartHomeLibrary.Commands;
using static SmartHomeTool.SmartHomeLibrary.Commands.DeviceVersion;

namespace SmartHomeTool.SmartHomeLibrary
{
	public partial class Commands
	{
		readonly Communication com;

		public Commands(Communication com)
		{
			this.com = com;
		}

		public bool SendGetDeviceAddress(uint packetId, uint encryptionKey, uint address, byte type,
				out List<uint> outAddress_)
		{
			uint outAddress;
			outAddress_ = new List<uint>();
			byte[] data = new byte[] { 0xfa, type };
			if (type == 1)
			{
				if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out _, out outAddress, out List<byte[]> dataOuts,
						1000 + com.DefaultReadTimeoutMs))
					return false;

				foreach (byte[] data_ in dataOuts)
					if (data_.Length == 5 && data_[0] == data[0] && (address == outAddress || address == Packets.Broadcast) && packetId == outPacketId)
						outAddress_.Add(Common.Uint32FromArrayBE(data_, 1));
				return outAddress_.Count > 0;
			}
			else
			{
				if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out _, out outAddress, out byte[] dataOut))
					return false;

				bool ok = dataOut.Length == 5 && dataOut[0] == data[0] && (address == outAddress || address == Packets.Broadcast) &&
						packetId == outPacketId;
				outAddress_.Add(Common.Uint32FromArrayBE(dataOut, 1));
				return ok;
			}
		}

		public bool SendDirectMode(uint packetId, uint encryptionKey, uint address, byte line)
		{
			byte[] data = new byte[] { 0xfb, line };
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out _, out uint outAddress, out byte[] dataOut))
				return false;

			return dataOut.Length == 2 && data[0] == dataOut[0] && (address == outAddress || address == Packets.Broadcast) &&
					packetId == outPacketId && dataOut[1] == 0;
		}

		public bool SendGetFlashMemory(uint packetId, uint encryptionKey, uint address,
				uint flashAddress, ushort length, out byte[] outData)
		{
			outData = Array.Empty<byte>();
			if (length == 0 || length > 256)
				return false;

			byte[] data = new byte[] { 0xfc, Common.Uint32_3Byte(flashAddress), Common.Uint32_2Byte(flashAddress),
					Common.Uint32_1Byte(flashAddress), Common.Uint32_0Byte(flashAddress),
					Common.Uint32_1Byte(length), Common.Uint32_0Byte(length)
			};
			com.SetReadTimeOut(Communication.ReadTimeoutEpromMs);
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out _, out uint outAddress, out byte[] dataOut))
			{
				com.SetDefaultReadTimeOut();
				return false;
			}

			com.SetDefaultReadTimeOut();
			bool ok = dataOut.Length == 2 + length && dataOut[0] == data[0] && address == outAddress && packetId == outPacketId;
			outData = new byte[length];
			Array.Copy(dataOut, 2, outData, 0, length);
			return ok;
		}

		public bool SendReset(uint packetId, uint encryptionKey, uint address, byte resetType, out byte error)
		{
			error = 0;
			byte[] data = new byte[] { 0xff, resetType };
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out _, out uint outAddress, out byte[] dataOut))
				return false;

			bool ok = dataOut.Length == 2 && dataOut[0] == data[0] && address == outAddress && packetId == outPacketId;
			if (ok)
				error = dataOut[1];
			return ok;
		}

		public class DeviceVersion
		{
			public enum ProgramStateType { Bootloader, Program, Unknown };

			public enum HardwareType1Enum
			{
				None = 0,
				Common = 1,
				DIN = 2,
				BOX = 3,
				RadioBOX = 4,
			};

			public enum HardwareType2Enum
			{
				None = 0,
				CU = 1,
				CU_WR = 2,
				Expander = 3,
				Radio = 4,
				Amplifier = 5,
				Acin = 41,
				Anin = 42,
				Anout = 43,
				Digin = 44,
				Dim = 45,
				Led = 46,
				Mul = 47,
				Rel = 48,
				Rol = 49,
				Temp = 50,
				Tablet = 81,
				TouchPanel = 82,
			};

			public enum HardwareType
			{
				None = (HardwareType1Enum.None << 8) | HardwareType2Enum.None,

				ISR_DIN_CU = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.CU,
				ISR_DIN_CU_WR = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.CU_WR,
				ISR_DIN_EKSP = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Expander,
				ISR_DIN_RADIO = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Radio,
				ISR_BOX_EKSP = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Expander,
				ISR_BOX_RADIO = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Radio,
				ISR_RADIO_AMP = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Amplifier,
				// tablet, ISR_BOX_TP4, ISR_RADIO_TP4

				ISR_DIN_ACIN = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Acin,
				ISR_DIN_ANIN = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Anin,
				ISR_DIN_ANOUT = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Anout,
				ISR_DIN_DIGIN = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Digin,
				ISR_DIN_DIM = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Dim,
				ISR_DIN_LED = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Led,
				ISR_DIN_MUL = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Mul,
				ISR_DIN_REL = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Rel,
				ISR_DIN_ROL = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Rol,
				ISR_DIN_TEMP = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Temp,

				ISR_BOX_ACIN = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Acin,
				ISR_BOX_ANIN = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Anin,
				ISR_BOX_ANOUT = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Anout,
				ISR_BOX_DIGIN = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Digin,
				ISR_BOX_DIM = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Dim,
				ISR_BOX_LED = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Led,
				ISR_BOX_MUL = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Mul,
				ISR_BOX_REL = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Rel,
				ISR_BOX_ROL = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Rol,
				ISR_BOX_TEMP = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Temp,

				ISR_RADIO_ACIN = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Acin,
				ISR_RADIO_DIGIN = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Digin,
				ISR_RADIO_DIM = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Dim,
				ISR_RADIO_LED = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Led,
				ISR_RADIO_MUL = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Mul,
				ISR_RADIO_REL = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Rel,
				ISR_RADIO_ROL = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Rol,
				ISR_RADIO_TEMP = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Temp,
			}

			public enum RealHardwareType
			{
				None = (HardwareType1Enum.None << 8) | HardwareType2Enum.None,

				ISR_DIN_CU = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.CU,
				ISR_DIN_CU_WR = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.CU_WR,
				ISR_DIN_EKSP = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Expander,
				ISR_DIN_RADIO = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Radio,
				ISR_BOX_EKSP = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Expander,
				ISR_BOX_RADIO = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Radio,
				ISR_RADIO_AMP = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Amplifier,
				// tablet, ISR_BOX_TP4, ISR_RADIO_TP4

				ISR_DIN_ACIN_4 = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Acin | (4 << 16),
				ISR_DIN_ANIN_4 = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Anin | (4 << 16),
				ISR_DIN_ANOUT_2 = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Anout | (2 << 16),
				ISR_DIN_DIGIN_4 = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Digin | (4 << 16),
				ISR_DIN_DIM_1 = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Dim | (1 << 16),
				ISR_DIN_LED_4 = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Led | (4 << 16),
				ISR_DIN_MUL = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Mul,
				ISR_DIN_REL_2 = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Rel | (2 << 16),
				ISR_DIN_ROL_1 = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Rol | (1 << 16),
				ISR_DIN_TEMP_4 = (HardwareType1Enum.DIN << 8) | HardwareType2Enum.Temp | (4 << 16),

				ISR_BOX_ACIN_3 = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Acin | (3 << 16),
				ISR_BOX_ANIN_3 = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Anin | (3 << 16),
				ISR_BOX_ANOUT_2 = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Anout | (2 << 16),
				ISR_BOX_DIGIN_3 = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Digin | (3 << 16),
				ISR_BOX_DIM_1 = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Dim | (1 << 16),
				ISR_BOX_LED_3 = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Led | (3 << 16),
				ISR_BOX_MUL = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Mul,
				ISR_BOX_REL_2 = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Rel | (2 << 16),
				ISR_BOX_ROL_1 = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Rol | (1 << 16),
				ISR_BOX_TEMP_2 = (HardwareType1Enum.BOX << 8) | HardwareType2Enum.Temp | (2 << 16),

				ISR_RADIO_ACIN_4 = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Acin | (4 << 16),
				ISR_RADIO_DIGIN_4 = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Digin | (4 << 16),
				ISR_RADIO_DIM_1 = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Dim | (1 << 16),
				ISR_RADIO_LED_3 = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Led | (3 << 16),
				ISR_RADIO_MUL = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Mul,
				ISR_RADIO_REL_2 = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Rel | (2 << 16),
				ISR_RADIO_ROL_1 = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Rol | (1 << 16),
				ISR_RADIO_TEMP_3 = (HardwareType1Enum.RadioBOX << 8) | HardwareType2Enum.Temp | (3 << 16),
			}

			public enum CpuType { None, Stm32, Esp32 };

			public DateTime ProgrammedProgram = new();
			public DateTime ProgramDateTime = new();
			public byte ProgramVersionMajor;
			public byte ProgramVersionMinor;
			public DateTime BootloaderDateTime = new();
			public byte BootloaderVersionMajor;
			public byte BootloaderVersionMinor;
			public HardwareType1Enum HardwareType1;
			public HardwareType2Enum HardwareType2;
			public byte HardwareSegmentsCount;
			public byte HardwareVersion;
			public uint Uptime; /// seconds
			public float Vin;
			public ProgramStateType ProgramState = ProgramStateType.Unknown;
			public int TimeResponse;

			//public static DeviceVersion CreateReadDevice(RealHardwareType realHardwareType)
			//{
			//	int hardwareType = (int)realHardwareType;
			//	return new DeviceVersion()
			//	{
			//		HardwareType1 = (HardwareType1Enum)((hardwareType >> 8) & 0xff),
			//		HardwareType2 = (HardwareType2Enum)(hardwareType & 0xff),
			//		HardwareTypeCount = (byte)((hardwareType >> 16) & 0xff),
			//	};
			//}

			public CpuType GetCpuType()
			{
				if (HardwareType1 == HardwareType1Enum.DIN && HardwareType2 == HardwareType2Enum.CU ||
						HardwareType1 == HardwareType1Enum.DIN && HardwareType2 == HardwareType2Enum.CU_WR ||
						HardwareType1 == HardwareType1Enum.BOX)
					return CpuType.Esp32;

				if (HardwareType1 == HardwareType1Enum.DIN)
					return CpuType.Stm32;

				return CpuType.None;
			}

			public string ToString_(bool showProgram = true, bool showBootloader = false,
					bool showProgramDate = false, bool showBootloaderDate = false,
					bool showProgramState = false, bool showDescription = false)
			{
				string s = "";
				if (showProgram)
				{
					if (showDescription)
						s += "program: ";
					s += "v" + ProgramVersionMajor + "." + ProgramVersionMinor + " ";
					if (showProgramDate)
						s += ProgrammedProgram.ToString("yyyy-MM-dd HH:mm") + " ";
				}
				if (showBootloader)
				{
					if (showDescription)
						s += "bootloader: ";
					s += "v" + BootloaderVersionMajor + "." + BootloaderVersionMinor + " ";
					if (showBootloaderDate)
						s += BootloaderDateTime.ToShortDateString() + " ";
				}
				if (showProgramState)
				{
					if (showDescription)
						s += "state: ";
					switch (ProgramState)
					{
						case ProgramStateType.Program: s += "program"; break;
						case ProgramStateType.Bootloader: s += "bootloader"; break;
						case ProgramStateType.Unknown: s += "unknown"; break;
					}
				}
				return s.TrimEnd();
			}

			public string ToTableString()
			{
				string s = "";
				s += ("v" + ProgramVersionMajor + "." + ProgramVersionMinor).PadLeft(6) + "  ";
				s += ProgramDateTime.ToShortDateString() + "  ";
				s += ProgrammedProgram.ToString("yyyy-MM-dd HH:mm") + " ";
				s += ("v" + BootloaderVersionMajor + "." + BootloaderVersionMinor).PadLeft(6) + " ";
				s += BootloaderDateTime.ToShortDateString() + " ";
				s += TimeSpan.FromSeconds(Uptime).ToString().PadLeft(13) + " ";
				s += Vin.ToString("0.00").PadLeft(5) + "V ";

				string t = "";
				switch (ProgramState)
				{
					case ProgramStateType.Program: t += "program"; break;
					case ProgramStateType.Bootloader: t += "{$red}bootloader{$default}"; break;
					case ProgramStateType.Unknown: t += "unknown"; break;
				}
				t = t.PadLeft(13);

				//if (IsInDirectMode.HasValue)
				//  t += IsInDirectMode.Value ? "  DirectMode" : "    Normal  ";

				return s + t;
			}
		}

		public bool SendGetDeviceVersion(uint packetId, uint encryptionKey, uint address, out DeviceVersion version)
		{
			version = new DeviceVersion();
			byte[] data = new byte[] { (byte)'v' };
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out uint _, out uint outAddress, out byte[] dataOut))
				return false;

			bool ok = dataOut.Length == 27 && dataOut[0] == data[0] && address == outAddress && packetId == outPacketId;
			if (ok)
			{
				if (DateTime.TryParse("20" + dataOut[1].ToString("d2") + '-' + dataOut[2] + '-' + dataOut[3] + ' ' + dataOut[4] + ':' + dataOut[5], out DateTime dt))
					version.ProgrammedProgram = dt;
				if (DateTime.TryParse("20" + dataOut[6].ToString("d2") + '-' + dataOut[7] + '-' + dataOut[8], out dt))
					version.ProgramDateTime = dt;
				version.ProgramVersionMajor = dataOut[9];
				version.ProgramVersionMinor = dataOut[10];
				if (DateTime.TryParse("20" + dataOut[11].ToString("d2") + '-' + dataOut[12] + '-' + dataOut[13], out dt))
					version.BootloaderDateTime = dt;
				version.BootloaderVersionMajor = dataOut[14];
				version.BootloaderVersionMinor = dataOut[15];
				version.HardwareType1 = (DeviceVersion.HardwareType1Enum)dataOut[16];
				version.HardwareType2 = (DeviceVersion.HardwareType2Enum)dataOut[17];
				version.HardwareSegmentsCount = dataOut[18];
				version.HardwareVersion = dataOut[19];
				version.Uptime = (uint)(dataOut[20] << 24 | dataOut[21] << 16 | dataOut[22] << 8 | dataOut[23]);
				version.Vin = ((dataOut[24] << 8) | dataOut[25]) / 1000f;
				if (dataOut[26] == 'b')
					version.ProgramState = DeviceVersion.ProgramStateType.Bootloader;
				else if (dataOut[26] == 'p')
					version.ProgramState = DeviceVersion.ProgramStateType.Program;
				version.TimeResponse = com.lastReceiveMiliseconds;
			}
			return ok;
		}
		
		public class CentralUnitDeviceItem
		{
			public enum LineNumber
			{
				None = 0,
				UART1 = 1,
				UART2 = 2,
				UART3 = 3,
				UART4 = 4,
				Radio = 64,
				LAN = 65,
			}

			public uint address;
			public LineNumber lineNumber;
			public HardwareType1Enum hardwareType1;
			public HardwareType2Enum hardwareType2;
			public byte hardwareSegmentsCount;
			public byte hardwareVersion;
			public List<CentralUnitDeviceItem> devicesItems = new();
		}

		public void SendSynchronize(uint packetId, uint encryptionKey, uint address)
		{
			SendSynchronize(packetId, encryptionKey, address, DateTime.Now);
		}

		public void SendSynchronize(uint packetId, uint encryptionKey, uint address, DateTime dt)
		{
			byte[] data = new byte[] { (byte)'S',
					(byte)(dt.Year - 2000), (byte)dt.Month, (byte)dt.Day, (byte)dt.DayOfWeek,
					(byte)dt.Hour, (byte)dt.Minute, (byte)dt.Second, (byte)(dt.Millisecond >> 8), (byte)(dt.Millisecond & 0xff) };
			com.SendPacket(packetId, encryptionKey, address, false, data);
		}
	}
}
