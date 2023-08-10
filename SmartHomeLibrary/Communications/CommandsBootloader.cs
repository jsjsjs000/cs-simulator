using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
	partial class Commands
	{
		public bool SendBootloader_ClearDeviceProgram(uint packetId, uint encryptionKey, uint address)
		{
			byte[] data = new byte[] { 0xf0, (byte)'C', (byte)'l', (byte)'e', (byte)'a', (byte)'r',
					(byte)'P', (byte)'r', (byte)'o', (byte)'g', (byte)'r', (byte)'a', (byte)'m' };
			com.SetReadTimeOut(Communication.ReadTimeoutEpromMs);
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out _, out uint outAddress, out byte[] dataOut))
			{
				com.SetDefaultReadTimeOut();
				return false;
			}

			com.SetDefaultReadTimeOut();
			return dataOut.Length == 2 && dataOut[0] == data[0] && address == outAddress && packetId == outPacketId;
		}

		public bool SendBootloader_ClearDeviceMemory(uint packetId, uint encryptionKey, uint address,
				ushort packetsCount, DateTime date, out byte error, out int wait)
		{
			error = 1;
			wait = 0;
			byte[] data = new byte[] { 0xf1, (byte)(packetsCount >> 8), (byte)(packetsCount & 0xff),
					(byte)(date.Year - 2000), (byte)date.Month, (byte)date.Day, (byte)date.Hour, (byte)date.Minute };
			com.SetReadTimeOut(Communication.ReadTimeoutEpromMs);
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out _, out uint outAddress, out byte[] dataOut))
			{
				com.SetDefaultReadTimeOut();
				return false;
			}

			com.SetDefaultReadTimeOut();
			bool ok = dataOut.Length == 4 && dataOut[0] == data[0] && address == outAddress && packetId == outPacketId;
			if (ok)
			{
				error = dataOut[1];
				wait = ((dataOut[2] << 8) | dataOut[3]) * 50;
			}
			return ok;
		}

		public bool SendBootloader_CheckIsClearDeviceMemory(uint packetId, uint encryptionKey, uint address,
				out byte error)
		{
			error = 1;
			byte[] data = new byte[] { 0xf2 };
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out _, out uint outAddress, out byte[] dataOut))
				return false;

			bool ok = dataOut.Length == 2 && dataOut[0] == data[0] && address == outAddress && packetId == outPacketId;
			if (ok)
				error = dataOut[1];
			return ok;
		}

		public void SendBootloader_SendPacketToDevice(uint packetId, uint encryptionKey, uint address,
				ushort packetNumber, byte[] packet)
		{
			byte[] data = new byte[5 + packet.Length];
			data[0] = 0xf3;
			data[1] = (byte)(packetNumber >> 8);
			data[2] = (byte)(packetNumber & 0xff);
			data[3] = (byte)(packet.Length >> 8);
			data[4] = (byte)(packet.Length & 0xff);
			Array.Copy(packet, 0, data, 5, packet.Length);
			com.SendPacket(packetId, encryptionKey, address, false, data);
			Thread.Sleep(Communication.ReadTimeoutEpromMs);
		}

		public bool SendBootloader_GetNotResolvedPacketsList(uint packetId, uint encryptionKey, uint address,
				out List<ushort> packetsList)
		{
			byte[] data = new byte[] { 0xf4 };
			packetsList = new List<ushort>();
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out _, out uint outAddress, out byte[] dataOut))
				return false;

			bool ok = dataOut.Length >= 2 && dataOut[0] == data[0] && address == outAddress && packetId == outPacketId &&
					dataOut.Length % 2 == 0 && Math.Min(dataOut[1], 21U) * 2 == dataOut.Length - 2;
			if (ok)
				for (int i = 0; i < Math.Min(dataOut[1], 20U); i++)
					packetsList.Add((ushort)((dataOut[2 + i * 2] << 8) | dataOut[2 + i * 2 + 1]));
			return ok;
		}

		public bool SendBootloader_EndDeviceProgramming(uint packetId, uint encryptionKey, uint address, uint crc32,
				out byte error)
		{
			error = 1;
			byte[] data = new byte[] { 0xf5, Common.Uint32_3Byte(crc32), Common.Uint32_2Byte(crc32),
					Common.Uint32_1Byte(crc32), Common.Uint32_0Byte(crc32) };
			com.SetReadTimeOut(Communication.ReadTimeoutEndProgramming);
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out _, out uint outAddress, out byte[] dataOut))
			{
				com.SetDefaultReadTimeOut();
				return false;
			}

			com.SetDefaultReadTimeOut();
			if (dataOut.Length >= 2)
				error = dataOut[1];
			return dataOut.Length == 2 && dataOut[0] == data[0] && address == outAddress && packetId == outPacketId;
		}

		public bool SendBootloader_SetHardwareIdAndDeviceAddress(uint packetId, uint encryptionKey, uint address,
				DeviceVersion.HardwareType1Enum hardwareType1, DeviceVersion.HardwareType2Enum hardwareType2,
				byte hardwareTypeCount, byte hardwareVersion, uint newAddress, out byte error)
		{
			error = 1;
			byte[] data = new byte[] { 0xf9,
					(byte)hardwareType1, (byte)hardwareType2, hardwareTypeCount, hardwareVersion,
					Common.Uint32_3Byte(newAddress), Common.Uint32_2Byte(newAddress),
					Common.Uint32_1Byte(newAddress), Common.Uint32_0Byte(newAddress) };
			com.SetReadTimeOut(Communication.ReadTimeoutEpromMs);
			if (!com.SendPacket(packetId, encryptionKey, address, data, out uint outPacketId, out _, out uint outAddress, out byte[] dataOut))
			{
				com.SetDefaultReadTimeOut();
				return false;
			}

			com.SetDefaultReadTimeOut();
			error = dataOut[1];
			return dataOut.Length == 2 && dataOut[0] == data[0] && address == outAddress && packetId == outPacketId;
		}
	}
}
