using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
	public class Packets
	{
		public const byte SOP = 0xA0;
		public const byte EOP = 0xA1;
		public const uint Broadcast = 0xffffffff;
		public const int PacketPreBytes = 15;
		public const int PacketPostBytes = 5;
		public const int EmptyFrameLength = PacketPreBytes + PacketPostBytes;
		public const int MaxFrameLength = 1024 - EmptyFrameLength - 1;

		static Random rnd = new Random();

		public static uint RandomPacketId()
		{
			return (uint)rnd.Next(int.MaxValue);
		}

		public enum PacketDirection { Out, In, None }

		public static byte[] EncodePacket(byte[] data, bool isAnswer)
		{
			if (data.Length == 0 || data.Length >= MaxFrameLength)
				return new byte[0];

			/// data2 - with FrameLength, PacketId, EncryptionKey, Address
			byte[] data2 = new byte[data.Length + 2];
			Array.Copy(data, 0, data2, 2, data.Length);
			data2[0] = (byte)((data.Length - 4 - 4 - 4) & 0xff);
			data2[1] = (byte)(((data.Length - 4 - 4 - 4) >> 8) & 0x3f);
			if (isAnswer)
				data2[1] |= 0x80;

			uint crc32 = Crc32.CalculateCrc32(0, data2);

			/// data3 - with SOP, CRC32, EOP
			byte[] data3 = new byte[data2.Length + 1 + 4 + 1];
			Array.Copy(data2, 0, data3, 1, data2.Length);
			data3[0] = SOP;
			data3[data3.Length - 5] = (byte)(crc32 >> 24);
			data3[data3.Length - 4] = (byte)(crc32 >> 16);
			data3[data3.Length - 3] = (byte)(crc32 >> 8);
			data3[data3.Length - 2] = (byte)(crc32 & 0xff);
			data3[data3.Length - 1] = EOP;

			return data3;
		}

		public static byte[] EncodePacket(uint packetId, uint encryptionKey,
				uint address, byte[] data, bool isAnswer)
		{
			byte[] dataOut = new byte[4 + 4 + 4 + data.Length];
			dataOut[0] = (byte)((packetId >> 24) & 0xff);
			dataOut[1] = (byte)((packetId >> 16) & 0xff);
			dataOut[2] = (byte)((packetId >> 8) & 0xff);
			dataOut[3] = (byte)(packetId & 0xff);
			dataOut[4] = (byte)((encryptionKey >> 24) & 0xff);
			dataOut[5] = (byte)((encryptionKey >> 16) & 0xff);
			dataOut[6] = (byte)((encryptionKey >> 8) & 0xff);
			dataOut[7] = (byte)(encryptionKey & 0xff);
			dataOut[8] = (byte)((address >> 24) & 0xff);
			dataOut[9] = (byte)((address >> 16) & 0xff);
			dataOut[10] = (byte)((address >> 8) & 0xff);
			dataOut[11] = (byte)(address & 0xff);
			Array.Copy(data, 0, dataOut, 12, data.Length);
			return EncodePacket(dataOut, isAnswer);
		}

		public static bool FindFrameAndDecodePacketInBuffer(byte[] data, int dataLength,
				out uint packetId, out uint encryptionKey, out uint address, out byte[] dataOut, out uint frameCrc32,
				out uint calculatedCrc32, out bool isAnswer)
		{
			dataOut = new byte[0];
			packetId = 0;
			encryptionKey = 0;
			address = 0;
			frameCrc32 = 0;
			calculatedCrc32 = 0;
			isAnswer = false;
			if (data.Length <= EmptyFrameLength)
				return false;
			
			int maxLength = Math.Min(data.Length, dataLength);
			for (int i = 0; i < maxLength; i++)
				if (data[i] == SOP && i + 15 + 5 + 1 <= maxLength)
				{
					int length = data[i + 1] | ((data[i + 2] & 0x3f) << 8);
					if (i + 15 + length + 5 <= maxLength && data[i + 15 + length + 5 - 1] == EOP)
					{
						frameCrc32 = (uint)(data[i + 15 + length + 5 - 4 - 1] << 24) | (uint)(data[i + 15 + length + 5 - 3 - 1] << 16) |
								(uint)(data[i + 15 + length + 5 - 2 - 1] << 8) | data[i + 15 + length + 5 - 1 - 1];
						calculatedCrc32 = Crc32.CalculateCrc32(0, data, i + 1, 2 + 4 + 4 + 4 + length);
						if (frameCrc32 == calculatedCrc32)
						{
							isAnswer = (data[i + 2] & 0x80) != 0;
							packetId = (uint)(data[i + 1 + 2 + 0] << 24) | (uint)(data[i + 1 + 2 + 1] << 16) |
									(uint)(data[i + 1 + 2 + 2] << 8) | data[i + 1 + 2 + 3];
							encryptionKey = (uint)(data[i + 1 + 2 + 4 + 0] << 24) | (uint)(data[i + 1 + 2 + 4 + 1] << 16) |
									(uint)(data[i + 1 + 2 + 4 + 2] << 8) | data[i + 1 + 2 + 4 + 3];
							address = (uint)(data[i + 1 + 2 + 4 + 4 + 0] << 24) | (uint)(data[i + 1 + 2 + 4 + 4 + 1] << 16) |
									(uint)(data[i + 1 + 2 + 4 + 4 + 2] << 8) | data[i + 1 + 2 + 4 + 4 + 3];
							dataOut = new byte[length];
							Array.Copy(data, i + 1 + 2 + 4 + 4 + 4, dataOut, 0, length);
							return true;
						}
					}
				}
			return false;
		}

		public static bool FindFrameAndDecodePacketInBuffer(byte[] data, int dataLength,
				out uint packetId, out uint encryptionKey, out uint address, out byte[] dataOut, out bool isAnswer)
		{
			return FindFrameAndDecodePacketInBuffer(data, dataLength, out packetId, out encryptionKey, out address,
					out dataOut, out uint frameCrc32, out uint calculatedCrc32, out isAnswer);
		}

		public static byte[] GetFirstPacketFromData(byte[] data, out byte[] rest)
		{
			int i1 = -1;
			int i2 = -1;
			for (int i = 0; i < data.Length; i++)
				if (data[i] == SOP)
				{
					i1 = i;
					break;
				}
			if (i1 >= 0)
				for (int i = i1; i < data.Length; i++)
					if (data[i] == EOP)
					{
						i2 = i;
						break;
					}
			if (i1 >= 0 && i2 >= 0 && i2 > i1)
			{
				byte[] dataOut = new byte[i2 + 1];
				rest = new byte[data.Length - i2 - 1];
				Array.Copy(data, dataOut, i2 + 1);
				Array.Copy(data, i2 + 1, rest, 0, data.Length - i2 - 1);
				return dataOut;
			}
			else
			{
				rest = (byte[])data.Clone();
				return new byte[0];
			}
		}
	}
}
