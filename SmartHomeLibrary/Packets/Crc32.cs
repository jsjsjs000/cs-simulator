using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
	class Crc32
	{
		const uint Crc32Seed = 0x7e5291fb;
		static readonly uint[] Crc32Table = new uint[256];

		static Crc32()
		{
			InitializeCrc32Table();
		}

		static void InitializeCrc32Table()
		{
			uint crc, d;
			for (uint i = 0; i < 256; i++)
			{
				crc = 0;
				d = i;
				for (int j = 0; j < 8; j++)
				{
					if (((crc ^ d) & 0x0001) != 0)
						crc = (crc >> 1) ^ Crc32Seed;
					else
						crc >>= 1;
					d >>= 1;
				}
				Crc32Table[i] = crc;
			}
		}

		public static void PrintCrc32Table()
		{
			for (uint i = 0; i < 256; i++)
			{
				System.Diagnostics.Debug.Write(String.Format("0x{0:x8}, ", Crc32Table[i]));
				if (i % 8 == 8 - 1)
					System.Diagnostics.Debug.WriteLine("");
			}
		}

		public static uint CalculateCrc32(uint crc, byte[] data)
		{
			foreach (byte d in data)
				crc = (crc >> 8) ^ Crc32Table[(crc ^ d) & 0xff];
			return crc;
		}

		public static uint CalculateCrc32(uint crc, byte[] data, int from, int length)
		{
			for (int i = from; i < from + length; i++)
				crc = (crc >> 8) ^ Crc32Table[(crc ^ data[i]) & 0xff];
			return crc;
		}

		public static uint CalculateCrc32_1Byte(uint crc, byte data)
		{
			return (crc >> 8) ^ Crc32Table[(crc ^ data) & 0xff];
		}
	}
}
