using InteligentnyDomSimulator.SmartHomeLibrary;
using SmartHomeTool.SmartHomeLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SmartHomeTool.SmartHomeLibrary.Commands.CentralUnitDeviceItem;
using static SmartHomeTool.SmartHomeLibrary.Commands.DeviceVersion;

namespace InteligentnyDomSimulator
{
	internal class CommunicationService : Communication
	{
		public int ResponseTime = 0;
		public static Dictionary<uint, DeviceItem> devicesItems = new();

		public CommunicationService(string ComName) : base()
		{
			this.ComName = ComName;
		}

		public static void InitializeDeviceConfiguration()
		{
				/// Adam Kukuc configuration: DIN-CU f938c9cd 70:b8:f6:87:62:e3
			AddDeviceItem(0x74024593, LineNumber.UART1, HardwareType1Enum.BOX, HardwareType2Enum.Temp, 2, 1, 0); // 7	- Salon i kuchnia - parter
			AddDeviceItem(0xedd60678, LineNumber.UART1, HardwareType1Enum.BOX, HardwareType2Enum.Temp, 2, 1, 0); // 10	- Korytarz, wejście, klatka schodowa - parter
			AddDeviceItem(0xda906314, LineNumber.UART1, HardwareType1Enum.BOX, HardwareType2Enum.Temp, 2, 1, 0); // 9	- WC - parter
			AddDeviceItem(0x6efa00b0, LineNumber.UART1, HardwareType1Enum.BOX, HardwareType2Enum.Temp, 2, 1, 0); // 4	- Sypialnia - I piętro
			AddDeviceItem(0x66a8c8e8, LineNumber.UART1, HardwareType1Enum.BOX, HardwareType2Enum.Temp, 2, 1, 0); // 5	- Łazienka - I piętro
			AddDeviceItem(0x262f6efd, LineNumber.UART1, HardwareType1Enum.BOX, HardwareType2Enum.Temp, 2, 1, 0); // 6	- Korytarz, klatka schodowa - I piętro
			AddDeviceItem(0x1b28c309, LineNumber.UART1, HardwareType1Enum.BOX, HardwareType2Enum.Temp, 2, 1, 0); // 1	- Poddasze
			AddDeviceItem(0x86246f30, LineNumber.UART1, HardwareType1Enum.BOX, HardwareType2Enum.Temp, 2, 1, 0); // 17	- Zewnętrzny
			AddDeviceItem(0xd054c0f1, LineNumber.UART1, HardwareType1Enum.BOX, HardwareType2Enum.Temp, 2, 1, 0); // 12 - CWU
			AddDeviceItem(0x6e9dd4c0, LineNumber.UART1, HardwareType1Enum.DIN, HardwareType2Enum.Temp, 4, 1, 0); // DIN- 1: Piwnica

			AddDeviceItem(0xbd2fa348, LineNumber.UART1, HardwareType1Enum.DIN, HardwareType2Enum.Rel, 2, 1, 0);  // 1.1 - Salon - parter
																																																			 		 // 1.2 - Korytarz - parter
			AddDeviceItem(0xbc9ebdef, LineNumber.UART1, HardwareType1Enum.DIN, HardwareType2Enum.Rel, 2, 1, 0);  // 2.1 - WC - parter
																																																		 			 // 2.2 - Sypialnia - I piętro
			AddDeviceItem(0xe423a30f, LineNumber.UART1, HardwareType1Enum.DIN, HardwareType2Enum.Rel, 2, 1, 0);  // 3.1 - Łazienka - I piętro
																																																					 // 3.2 - Korytarz - I piętro
			AddDeviceItem(0xf12e11f2, LineNumber.UART1, HardwareType1Enum.DIN, HardwareType2Enum.Rel, 2, 1, 0);  // 4.1 - Poddasze
																																																					 // 4.2 - <brak>
			AddDeviceItem(0x7bf90ca3, LineNumber.UART1, HardwareType1Enum.DIN, HardwareType2Enum.Rel, 2, 1, 0);  // 9.1 - CWU Boiler
		}

		static void AddDeviceItem(uint address, LineNumber line, HardwareType1Enum type1, HardwareType2Enum type2, byte hardwareSegmentsCount,
				byte hardwareVersion, ushort _)
		{
			DeviceItem deviceItem = new()
			{
				address = address,
				lineNumber = line,
				hardwareType1 = type1,
				hardwareType2 = type2,
				hardwareSegmentsCount = hardwareSegmentsCount,
				hardwareVersion = hardwareVersion,
			};
			if (type2 == HardwareType2Enum.Temp)
			{
				deviceItem.status = new TemperatureStatus();
				if (hardwareSegmentsCount == 2)
					((TemperatureStatus)deviceItem.status).temperatures = new ushort[] { 21, 24 };
				else if (hardwareSegmentsCount == 4)
					((TemperatureStatus)deviceItem.status).temperatures = new ushort[] { 21, 22, 23, 24 };
				else
					return;
			}
			else if (type2 == HardwareType2Enum.Rel)
			{
				deviceItem.status = new RelayStatus();
				((RelayStatus)deviceItem.status).relays = new bool[] { false, false };
			}
			devicesItems.Add(address, deviceItem);
		}

		protected override void ThreadProc()
		{
			ExitedThread = false;
			Common.SetDateFormat();

			while (!ExitThread)
			{
				try
				{
					if (com != null && com.IsOpen)
					{
						if (com.BytesToRead > 0)
						{
							if (receiveBufferIndex + com.BytesToRead >= receiveBuffer.Length)
								receiveBufferIndex = 0;  /// buffer overflow

							int readedBytes = com.Read(receiveBuffer, receiveBufferIndex, com.BytesToRead);
							byte[] received = new byte[readedBytes];
							Array.Copy(receiveBuffer, receiveBufferIndex, received, 0, readedBytes);
							receiveBufferIndex += readedBytes;

							lock (packetsLogQueue)
								packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Packet, DateTime.Now, received, Packets.PacketDirection.In));

							AnalizeAndAnswer();
						}
						else
							Thread.Sleep(2);
					}
					else
						Thread.Sleep(2);
				}
				catch { }
			}

			ExitedThread = true;
		}

		void AnalizeAndAnswer()
		{
			if (Packets.FindFrameAndDecodePacketInBuffer(receiveBuffer, receiveBuffer.Length, out uint packetId,
					out uint encryptionKey, out uint address, out byte[] data, out uint frameCrc, out uint calculatedCrc,
					out bool isAnswer) && data.Length >= 1 && !isAnswer)
			{
				if (address == Packets.Broadcast)
				{
					NoAnswer();
					return;
				}

				string comment = PacketsComments.GetCommentToCorrectFrame(packetId, encryptionKey, address, data, isAnswer, true);
				if (comment.Length > 0)
					lock (packetsLogQueue)
						packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Debug, DateTime.Now, Array.Empty<byte>(),
								Packets.PacketDirection.None, " " + comment, false));

				if (ResponseTime > 0)
					Thread.Sleep(ResponseTime);

				byte command = data[0];
				// Command: 0xfa - Get Device Address
				if (command == 0xfa && data.Length == 2)
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command,
							Common.Uint32_3Byte(address), Common.Uint32_2Byte(address), Common.Uint32_1Byte(address), Common.Uint32_0Byte(address),
							0, 0, 2, 0 }); // hardware type 1, hardware type 2, segments, hardware type $$
														 // Command: 0xfb - Get Direct Mode (Transparent Mode)
				else if (command == 0xfb && data.Length == 1)
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command, 0, 0 });
				// Command: 0xfb - Set Direct Mode (Transparent Mode)
				else if (command == 0xfb && data.Length == 2)
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command, 0 });
				else if (command == 0xfc && data.Length == 7)
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command, 0, 0 });
				/// Command: 0xff - Reset Device
				else if (command == 0xff && data.Length == 2)
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command, 0 });
				/// Command: 'v' - Get Device Program, Bootloader Version, Date Programming And Device Serial Number
				else if (command == (byte)'v' && data.Length == 1)
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command,
								(byte)(DateTime.Now.Year - 2000), (byte)(DateTime.Now.Month), (byte)(DateTime.Now.Day),
								(byte)(DateTime.Now.Hour), (byte)(DateTime.Now.Minute),
								(byte)(DateTime.Now.Year - 2000), (byte)(DateTime.Now.Month), (byte)(DateTime.Now.Day), 1, 0,
								(byte)(DateTime.Now.Year - 2000), (byte)(DateTime.Now.Month), (byte)(DateTime.Now.Day), 1, 1,
								0x12, 0x34, 0x56, 0x78, 0, 0, 0, 10, (byte)'p' });
				/// Command: 'p' - Ping
				else if (command == (byte)'p' && (data.Length >= 0 && data.Length <= 255))
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command });
				/// Command: 'S' - Send synchronization
				else if (command == 'S' && data.Length == 10)
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command, 0 });

				/// Command: 'r' - Get Relays Status (only REL)
				else if (command == 'r' && data.Length == 1 && devicesItems.ContainsKey(address))
				{
					DeviceItem device = devicesItems[address];
					if (device.status is RelayStatus rels && !device.status!.error)
						SendPacket(packetId, encryptionKey, address, true, new byte[] { command,
								Common.Uint32_3Byte(1234), Common.Uint32_2Byte(1234), Common.Uint32_1Byte(1234), Common.Uint32_0Byte(1234), // uptime
								Common.Uint32_1Byte(3300), Common.Uint32_0Byte(3300), // voltage
								2, Convert.ToByte(rels.relays[0]), Convert.ToByte(rels.relays[1]) });
				}
				/// Command: 'r' - Set Relay State (only REL)
				else if (command == 'r' && data.Length == 3 && devicesItems.ContainsKey(address))
				{
					DeviceItem device = devicesItems[address];
					if (device.status is RelayStatus rels && data.Length == 3 && data[1] < rels.relays.Length)
						rels.relays[data[1]] = Convert.ToBoolean(data[2]);
					if (!device.status!.error)
						SendPacket(packetId, encryptionKey, address, true, new byte[] { command,
0 }); // $$
				}
				/// Command: 't' - Temperature (only TEMP)
				else if (command == 't' && data.Length == 1 && devicesItems.ContainsKey(address))
				{
					byte[] temperatures = Array.Empty<byte>();
					DeviceItem device = devicesItems[address];
					TemperatureStatus? temps = device.status as TemperatureStatus;
					if (temps != null && device.hardwareSegmentsCount == 2)
						temperatures = new byte[] {
								Common.Uint32_1Byte((uint)temps.temperatures[0] << 4), Common.Uint32_0Byte((uint)temps.temperatures[0] << 4),
								Common.Uint32_1Byte((uint)temps.temperatures[1] << 4), Common.Uint32_0Byte((uint)temps.temperatures[1] << 4)};
					else if (temps != null && device.hardwareSegmentsCount == 4)
						temperatures = new byte[] {
								Common.Uint32_1Byte((uint)temps.temperatures[0] << 4), Common.Uint32_0Byte((uint)temps.temperatures[0] << 4),
								Common.Uint32_1Byte((uint)temps.temperatures[1] << 4), Common.Uint32_0Byte((uint)temps.temperatures[1] << 4),
								Common.Uint32_1Byte((uint)temps.temperatures[2] << 4), Common.Uint32_0Byte((uint)temps.temperatures[2] << 4),
								Common.Uint32_1Byte((uint)temps.temperatures[3] << 4), Common.Uint32_0Byte((uint)temps.temperatures[3] << 4) };
					byte[] send = new byte[] { command,
								Common.Uint32_3Byte(1234), Common.Uint32_2Byte(1234), Common.Uint32_1Byte(1234), Common.Uint32_0Byte(1234), // uptime
								Common.Uint32_1Byte(3300), Common.Uint32_0Byte(3300),	// voltage
								device.hardwareSegmentsCount };
					send = send.Concat(temperatures).ToArray();
					if (temperatures.Length != 0 && !device.status!.error)
						SendPacket(packetId, encryptionKey, address, true, send);
				}
				/// Command: "RTC" - Get RTC date and time (only CU)
				else if (command == 'R' && data.Length == 4 && data[1] == 'T' && data[2] == 'C')
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command, (byte)'T', (byte)'C', 0, 0, 0, 0, 0, 0, 0, 0 });
				/// Command: 'g' - Get CU Status (only CU)
				else if (command == 'g' && data.Length == 1)
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command, 0 });
				/// Command: "sREL" - Set Relay State (only CU)
				else if (command == 's' && data.Length == 10 && data[1] == 'R' && data[2] == 'E' && data[3] == 'L')
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command, (byte)'R', (byte)'E', (byte)'L', 0 });
				/// Command: "gREL" - Get Relay State (only CU)
				else if (command == 'g' && data.Length == 9 && data[1] == 'R' && data[2] == 'E' && data[3] == 'L')
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command, (byte)'R', (byte)'E', (byte)'L', 0 });
				/// Command: "gCOMP" - Get Visual Components Configuration (only CU)
				else if (command == 'g' && data.Length == 10 && data[1] == 'C' && data[2] == 'O' && data[3] == 'M' && data[4] == 'P')
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command, (byte)'C', (byte)'O', (byte)'M', (byte)'P', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
				/// Command: "sCOMP" - Set Visual Components Configuration (only CU)
				else if (command == 'S' && data.Length == 21 && data[1] == 'C' && data[2] == 'O' && data[3] == 'M' && data[4] == 'P')
					SendPacket(packetId, encryptionKey, address, true, new byte[] { command, (byte)'C', (byte)'O', (byte)'M', (byte)'P', 0 });

				receiveBufferIndex = 0;
			}
			else
			{
				string comment = PacketsComments.GetCommentToErrorFrame(frameCrc, calculatedCrc);
				if (comment.Length > 0)
					lock (packetsLogQueue)
						packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Debug, DateTime.Now, Array.Empty<byte>(),
								Packets.PacketDirection.None, " " + comment, true));
			}
		}

		void NoAnswer()
		{
			receiveBufferIndex = 0;
		}
	}
}
