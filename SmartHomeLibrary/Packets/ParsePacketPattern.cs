using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace SmartHomeTool.SmartHomeLibrary
{
	public class ParsePacketPatternItem
	{
		public enum Type { Bytes, SomeByte, AnyBytes };

		public Type type;
		public List<byte> Bytes;
		public ushort lengthFrom;
		public ushort lengthTo;

		public ParsePacketPatternItem(Type type, List<byte> Bytes, ushort lengthFrom, ushort lengthTo)
		{
			this.type = type;
			this.Bytes = Bytes;
			this.lengthFrom = lengthFrom;
			this.lengthTo = lengthTo;
		}
	}

	public class ParsePacketPattern
	{
		public List<ParsePacketPatternItem> list = new List<ParsePacketPatternItem>();

			/// return error string
		public static string ParsePacketPatternFromString(string s, out ParsePacketPattern pattern)
		{
			pattern = new ParsePacketPattern();
			string[] ss = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			pattern.list = new List<ParsePacketPatternItem>();
			foreach (string ss_ in ss)
			{
				byte d;
				if (ss_ == "?")
					pattern.list.Add(new ParsePacketPatternItem(ParsePacketPatternItem.Type.SomeByte, new List<byte>() { }, 0, 0));
				else if (ss_.Length == 3 && ((ss_[0] == '"' && ss_[2] == '"') || (ss_[0] == '\'' && ss_[2] == '\'')))
					pattern.list.Add(new ParsePacketPatternItem(ParsePacketPatternItem.Type.Bytes, new List<byte>() { (byte)ss_[1] }, 0, 0));
				else if (byte.TryParse(ss_, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out d))
					pattern.list.Add(new ParsePacketPatternItem(ParsePacketPatternItem.Type.Bytes, new List<byte>() { d }, 0, 0));
				else if (ParsePacket.Constats.ContainsKey(ss_))
					pattern.list.Add(new ParsePacketPatternItem(ParsePacketPatternItem.Type.Bytes, new List<byte>() { ParsePacket.Constats[ss_] }, 0, 0));
				else
				{
					ushort from, to;
					List<byte> bytes;
					if (ParseAnyBytesString(ss_, out from, out to))
						pattern.list.Add(new ParsePacketPatternItem(ParsePacketPatternItem.Type.AnyBytes, new List<byte>() { }, from, to));
					else if (ParseRangeBytesString(ss_, out bytes))
						pattern.list.Add(new ParsePacketPatternItem(ParsePacketPatternItem.Type.Bytes, bytes, 0, 0));
					else
					{
						pattern.list = new List<ParsePacketPatternItem>();
						return "Error: Can't parse packet from: '" + ss_ + "'";
					}
				}
			}
			return "";
		}

		public bool IsPacketMatchToPattern(byte[] data)
		{
			int i = 0;
			int iMin = 0;
			int iMax = 0;
			foreach (ParsePacketPatternItem item in list)
			{
				if (i >= list.Count)
					return false;
				if (item.type == ParsePacketPatternItem.Type.Bytes)
				{
					if (i >= data.Length || !item.Bytes.Contains(data[i]))
						return false;
				}
				else if (item.type == ParsePacketPatternItem.Type.AnyBytes)
				{
					iMin = item.lengthFrom;
					iMax = item.lengthTo;
					i--;
				}
				i++;
			}
			return i == data.Length || i + iMin <= data.Length && i + iMax >= data.Length;
		}

		static bool ParseAnyBytesString(string s, out ushort from, out ushort to)
		{
			from = 0;
			to = 0;
			if (s.Length < 3)
				return false;
			if (s[0] != '(' || s[s.Length - 1] != ')')
				return false;
			s = s.Substring(1, s.Length - 2);
			string[] ss = s.Split('-');
			if (ss.Length == 1)
			{
				ushort fromTo;
				if (ushort.TryParse(ss[0], out fromTo))
				{
					from = fromTo;
					to = fromTo;
					return true;
				}
			}
			else if (ss.Length == 2)
			{
				ushort from_, to_;
				if (ushort.TryParse(ss[0], out from_) && ushort.TryParse(ss[1], out to_) && from <= to)
				{
					from = from_;
					to = to_;
					return true;
				}
			}
			return false;
		}

		static bool ParseRangeBytesString(string s, out List<byte> list)
		{
			list = new List<byte>();
			if (s.Length < 3)
				return false;
			if (s[0] != '[' || s[s.Length - 1] != ']')
				return false;
			s = s.Substring(1, s.Length - 2);
			foreach (string constant in ParsePacket.Constats.Keys)
				s = s.Replace(constant, ParsePacket.Constats[constant].ToString());
			if (!AddressRange.Parse(s, out list))
				return false;
			return list.Count > 0;
		}
	}
}

/// ?                           - one some byte
/// (128) or (2-256)            - any array with length 128 bytes or between 2 and 256 bytes
/// [2] or [2,3,4] or [2-4,7-9] - bytes list
