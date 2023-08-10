using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InteligentnyDomRelay.SmartHomeLibrary;
using static ConsoleSmartHomeTool.Configuration;
using static SmartHomeTool.SmartHomeLibrary.Commands.DeviceVersion;

namespace SmartHomeTool.SmartHomeLibrary
{
	public partial class Commands
	{
		public class CentralUnitStatus
		{
			enum StatusType { Temperature = 0x40, Relay = 0x41, HeatingVisualComponent = 0x50 }

			public uint address;
			public bool initialized;
			public bool error;
			public uint uptime;
			public float vin;

			public static bool ParseFromBytes(byte[] bytes, out List<CentralUnitStatus> status,
					out List<HeatingVisualComponent> heatingVisualComponents,
					bool detail, out int itemsCount, out uint uptime, out float vin)
			{
				itemsCount = 0;
				uptime = uint.MaxValue;
				vin = 0;
				int j = 0;
				status = new List<CentralUnitStatus>();
				heatingVisualComponents = new List<HeatingVisualComponent>();
				List<CentralUnitStatus> statusTmp = new List<CentralUnitStatus>();
				if (bytes.Length < 1 + 2 + (detail ? 4 + 2 : 0))
					return false;

				int itemsReceived = bytes[j++];
				itemsCount = (bytes[j++] << 8) | bytes[j++];

				if (detail)
				{
					uptime = (uint)((bytes[j++] << 24) | (bytes[j++] << 16) | (bytes[j++] << 8) | bytes[j++]);
					vin = ((bytes[j++] << 8) | bytes[j++]) / 1000f;
				}

				for (int i = 0; i < itemsReceived; i++)
				{
					if (j + 5 >= bytes.Length)
						return false;

					StatusType type = (StatusType)bytes[j++];
					uint address = (uint)((bytes[j++] << 24) | (bytes[j++] << 16) | (bytes[j++] << 8) | bytes[j++]);
					if (type == StatusType.Temperature || type == StatusType.Relay)
					{
						if (j + 1 + 1 + (detail ? 4 + 2 + 1 : 0) >= bytes.Length)
							return false;

						bool initialized = Convert.ToBoolean(bytes[j++]);
						bool error = Convert.ToBoolean(bytes[j++]);
						uint uptime_ = 0;
						float vin_ = 0;
						byte communicationPercent_;
						if (detail)
						{
							uptime_ = (uint)((bytes[j++] << 24) | (bytes[j++] << 16) | (bytes[j++] << 8) | bytes[j++]);
							vin_ = ((bytes[j++] << 8) | bytes[j++]) / 1000f;
							communicationPercent_ = bytes[j++];
						}
						 
						if (j + 1 >= bytes.Length)
							return false;
						byte segmentsCount = bytes[j++];

						CentralUnitStatus statusItem;
						if (type == StatusType.Temperature)
						{
							if (!TemperatureStatus.ParseFromBytes(bytes[j..(j + segmentsCount * TemperatureStatus.BytesCount)],
									segmentsCount, out statusItem))
								return false;
							j += segmentsCount * TemperatureStatus.BytesCount;
						}
						else if (type == StatusType.Relay)
						{
							if (!RelayStatus.ParseFromBytes(bytes[j..(j + segmentsCount * RelayStatus.BytesCount)],
									segmentsCount, out statusItem))
								return false;
							j += segmentsCount * RelayStatus.BytesCount;
						}
						else
							return false;

						statusItem.address = address;
						statusItem.initialized = initialized;
						statusItem.error = error;
						statusItem.uptime = uptime_;
						statusItem.vin = vin_;
						statusTmp.Add(statusItem);
					}
					else if (type == StatusType.HeatingVisualComponent)
					{
						byte segment = bytes[j++];
						HeatingVisualComponentControl.Mode mode = (HeatingVisualComponentControl.Mode)bytes[j++];
						float temperature = ((bytes[j++] << 8) | bytes[j++]) / 16f;

						HeatingVisualComponent heatingVisualComponent = new HeatingVisualComponent()
						{
							DeviceItem = new Devices()
							{
								Address = address,
							},
							DeviceSegment = segment,
							Control = new HeatingVisualComponentControl()
							{
								HeatingMode = mode,
							},
						};
						if (heatingVisualComponent.Control.HeatingMode == HeatingVisualComponentControl.Mode.Auto)
							heatingVisualComponent.Control.DayTemperature = temperature;
						else if (heatingVisualComponent.Control.HeatingMode == HeatingVisualComponentControl.Mode.Manual)
							heatingVisualComponent.Control.ManualTemperature = temperature;
						heatingVisualComponents.Add(heatingVisualComponent);
					}
					else
						return false;
				}
				if (j != bytes.Length)
					return false;

				status = statusTmp;
				return true;
			}
		}

		public class RelayStatus : CentralUnitStatus
		{
			public const int BytesCount = 1;
			public bool[]? relaysStates;

			public static bool ParseFromBytes(byte[] bytes, int segmentsCount, out CentralUnitStatus relaysStatus)
			{
				int j = 0;
				relaysStatus = new();
				RelayStatus relaysStatus_ = new RelayStatus();
				if (segmentsCount * BytesCount != bytes.Length)
					return false;

				relaysStatus_.relaysStates = new bool[segmentsCount];
				for (int i = 0; i < segmentsCount; i++)
					relaysStatus_.relaysStates[i] = Convert.ToBoolean(bytes[j++]);

				relaysStatus = relaysStatus_;
				return true;
			}
		}

		public class TemperatureStatus : CentralUnitStatus
		{
			public const int BytesCount = 2;
			public float[]? temperatures;

			public static bool ParseFromBytes(byte[] bytes, int segmentsCount, out CentralUnitStatus temperatureStatus)
			{
				int j = 0;
				temperatureStatus = new();
				TemperatureStatus temperatureStatus_ = new();
				if (segmentsCount * BytesCount != bytes.Length)
					return false;

				temperatureStatus_.temperatures = new float[segmentsCount];
				for (int i = 0; i < segmentsCount; i++)
				{
					temperatureStatus_.temperatures[i] = (short)((bytes[j++] << 8) | bytes[j++]);
					if (temperatureStatus_.temperatures[i] != 0x7fff)
						temperatureStatus_.temperatures[i] /= 16f;
				}

				temperatureStatus = temperatureStatus_;
				return true;
			}
		}

		public bool SendGetCentralUnitStatus(uint packetId, uint encryptionKey, uint address,
				out List<CentralUnitStatus> status, out List<HeatingVisualComponent> heatingVisualComponents,
				int fromItem, bool detail, out int itemsCount, out uint cuUptime, out float cuVin)
		{
			status = new List<CentralUnitStatus>();
			heatingVisualComponents = new List<HeatingVisualComponent>();
			itemsCount = 0;
			cuUptime = uint.MaxValue;
			cuVin = 0;
			byte[] data = new byte[] { (byte)'g', (byte)(fromItem >> 8), (byte)(fromItem & 0xff), (byte)(detail ? 1 : 0) };
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out uint _, out uint outAddress, out byte[] dataOut))
				return false;

			bool ok = dataOut.Length >= 1 && dataOut[0] == data[0] && address == outAddress && packetId == outPacketId;
			if (ok)
				ok = CentralUnitStatus.ParseFromBytes(dataOut[1..], out status, out heatingVisualComponents,
						detail, out itemsCount, out cuUptime, out cuVin);
			return ok;
		}

		public bool SendSetRelay(uint packetId, uint encryptionKey, uint address, uint relayAddress, byte segment, bool set)
		{
			byte[] data = new byte[] {
					(byte)'s', (byte)'R', (byte)'E', (byte)'L', Common.Uint32_3Byte(relayAddress),
					Common.Uint32_2Byte(relayAddress), Common.Uint32_1Byte(relayAddress), Common.Uint32_0Byte(relayAddress),
					segment, (byte)(set ? 1 : 0) };
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out uint _, out uint outAddress, out byte[] dataOut))
				return false;

			return dataOut.Length >= 5 && dataOut[0] == data[0] && dataOut[1] == data[1] && dataOut[2] == data[2] && dataOut[3] == data[3] &&
					address == outAddress && packetId == outPacketId && dataOut[4] == 0;
		}

		public bool SendGetRelay(uint packetId, uint encryptionKey, uint address, uint relayAddress, byte segment, out bool? set)
		{
			set = null;
			byte[] data = new byte[] {
					(byte)'g', (byte)'R', (byte)'E', (byte)'L', Common.Uint32_3Byte(relayAddress),
					Common.Uint32_2Byte(relayAddress), Common.Uint32_1Byte(relayAddress), Common.Uint32_0Byte(relayAddress), segment };
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out uint _, out uint outAddress, out byte[] dataOut))
				return false;

			if (dataOut[4] == 0)
				set = false;
			else if (dataOut[4] == 1)
				set = true;
			return dataOut.Length >= 5 && dataOut[0] == data[0] && dataOut[1] == data[1] && dataOut[2] == data[2] && dataOut[3] == data[3] &&
					address == outAddress && packetId == outPacketId;
		}

		public bool SendSetConfiguration(uint packetId, uint encryptionKey, uint address, uint relayAddress, byte segment,
				HeatingVisualComponent heating)
		{
			byte[] data = new byte[] {
					(byte)'s', (byte)'C', (byte)'O', (byte)'N', (byte)'F', Common.Uint32_3Byte(relayAddress),
					Common.Uint32_2Byte(relayAddress), Common.Uint32_1Byte(relayAddress), Common.Uint32_0Byte(relayAddress), segment,
					(byte)heating.Control.HeatingMode, (byte)heating.Control.DayFrom.Hours, (byte)heating.Control.DayFrom.Minutes,
					(byte)heating.Control.NightFrom.Hours, (byte)heating.Control.NightFrom.Minutes,
					Common.Uint32_1Byte((uint)(heating.Control.ManualTemperature * 10f)), Common.Uint32_0Byte((uint)(heating.Control.ManualTemperature * 10f)),
					Common.Uint32_1Byte((uint)(heating.Control.DayTemperature * 10f)), Common.Uint32_0Byte((uint)(heating.Control.DayTemperature * 10f)),
					Common.Uint32_1Byte((uint)(heating.Control.NightTemperature * 10f)), Common.Uint32_0Byte((uint)(heating.Control.NightTemperature * 10f)) };
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out uint _, out uint outAddress, out byte[] dataOut))
				return false;

			return dataOut.Length >= 6 && dataOut[0] == data[0] && dataOut[1] == data[1] && dataOut[2] == data[2] && dataOut[3] == data[3] &&
					dataOut[4] == data[4] && address == outAddress && packetId == outPacketId && dataOut[5] == 0;
		}

		public bool SendGetConfiguration(uint packetId, uint encryptionKey, uint address, uint relayAddress, byte segment,
				out HeatingVisualComponent heating)
		{
			heating = null;
			byte[] data = new byte[] {
					(byte)'g', (byte)'C', (byte)'O', (byte)'N', (byte)'F', Common.Uint32_3Byte(relayAddress),
					Common.Uint32_2Byte(relayAddress), Common.Uint32_1Byte(relayAddress), Common.Uint32_0Byte(relayAddress), segment };
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out uint _, out uint outAddress, out byte[] dataOut))
				return false;

			if (dataOut.Length != 17 || dataOut[5] != 0)
				return false;

			heating = new HeatingVisualComponent
			{
				DeviceItem = new Devices()
				{
					Address = relayAddress,
				},
				DeviceSegment = segment,
				Control = new HeatingVisualComponentControl()
				{
					HeatingMode = (HeatingVisualComponentControl.Mode)dataOut[6],
					DayFrom = new TimeSpan(dataOut[7], dataOut[8], 0),
					NightFrom = new TimeSpan(dataOut[9], dataOut[10], 0),
					ManualTemperature = ((dataOut[11] << 8) | dataOut[12]) / 10f,
					DayTemperature = ((dataOut[13] << 8) | dataOut[14]) / 10f,
					NightTemperature = ((dataOut[15] << 8) | dataOut[16]) / 10f,
				},
			};
			return dataOut.Length >= 5 && dataOut[0] == data[0] && dataOut[1] == data[1] && dataOut[2] == data[2] && dataOut[3] == data[3] &&
					dataOut[4] == data[4] && address == outAddress && packetId == outPacketId;
		}
	}
}
