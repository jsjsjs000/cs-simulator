using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace SmartHomeTool.SmartHomeLibrary
{
	class ParsePacket
	{
		public readonly static Dictionary<string, byte> Constats = new Dictionary<string, byte>()
		{
			{"SOP", Packets.SOP},
			{"EOP", Packets.EOP}
		};

			/// return error string
		public static string ParsePacketFromString(string s, out byte[] data)
		{
			string[] ss = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			data = new byte[4 * 1024];
			int i = 0;
			foreach (string ss_ in ss)
			{
				if (ss_.Length == 3 && ((ss_[0] == '"' && ss_[2] == '"') || (ss_[0] == '\'' && ss_[2] == '\'')))
					data[i++] = (byte)ss_[1];
				else if (ss_.Length == 8 && uint.TryParse(ss_, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint u))
				{
					data[i++] = Common.Uint32_3Byte(u);
					data[i++] = Common.Uint32_2Byte(u);
					data[i++] = Common.Uint32_1Byte(u);
					data[i++] = Common.Uint32_0Byte(u);
				}
				else if (byte.TryParse(ss_, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out byte d))
					data[i++] = d;
				else if (Constats.ContainsKey(ss_))
					data[i++] = Constats[ss_];
				else
				{
					data = new byte[0];
					return "Error: Can't parse packet from: '" + ss_ + "'";
				}
			}
			Array.Resize(ref data, i);
			return "";
		}
	}
}
