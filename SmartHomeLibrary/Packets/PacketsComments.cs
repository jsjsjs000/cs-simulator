using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
	public class PacketDecodeAndCommentItem
	{
		const string Separator = "_$$_";
		const string ReturnSeparator = "_$R$_";

		public bool IsAnswer;
		public ParsePacketPattern PacketPattern;
		public List<string> CommentPattern = new();

		public static PacketDecodeAndCommentItem? ParseFromString(string s, out string error)
		{
			string[] ss = s.Split(new string[] { Separator }, StringSplitOptions.None);
			if (ss.Length != 3)
			{
				error = "Error: '" + s + "' must have 3 parts.";
				return null;
			}

			PacketDecodeAndCommentItem item = new();
			if (ss[0].ToLower() == "request")
				item.IsAnswer = false;
			else if (ss[0].ToLower() == "answer")
				item.IsAnswer = true;
			else
			{
				error = "Error: First part '" + ss[0] + "' must be: 'request' or 'answer'.";
				return null;
			}
			error = ParsePacketPattern.ParsePacketPatternFromString(ss[1], out item.PacketPattern);
			if (error.Length > 0)
				return null;
			string[] comment = ss[2].Split(new string[] { ReturnSeparator }, StringSplitOptions.None);
			item.CommentPattern = new List<string>(comment);
			return item;
		}

		public override string ToString()
		{
			string s = "";
			//$$
			return s;
		}
	}

	class PacketsComments
	{
		public static List<PacketDecodeAndCommentItem> PacketDecodeAndCommentItems = new();

		public static string GetCommentToFrame(bool isAnswer, byte[] data)
		{
			foreach (PacketDecodeAndCommentItem item in PacketDecodeAndCommentItems)
				if (item.IsAnswer == isAnswer && item.PacketPattern.IsPacketMatchToPattern(data))
				{
					string s = isAnswer ? "Answer: " : "Request: ";
					int i = 0;
					foreach (string t in item.CommentPattern)
						if (i++ == 0)
							s += t;
						else
							s += Environment.NewLine + t;
					s = ReplaceCommentPattern(s, data);
					return s;
				}
			return "";
		}

		public static string DecodeFrameAndGetComment(byte[] data, out bool error)
		{
			if (!Packets.FindFrameAndDecodePacketInBuffer(data, data.Length, out _, out _, out _,
					out byte[] dataOut, out uint frameCrc32, out uint calculatedCrc32, out bool isAnswer))
			{
				error = true;
				if (frameCrc32 != calculatedCrc32)
					return String.Format("Error: Frame CRC32={0:x8} != calculated CRC32={1:x8}.",
							frameCrc32, calculatedCrc32);
				else
					return "Error: No packet found.";
			}

			error = false;
			return GetCommentToFrame(isAnswer, dataOut);
		}

		public static string GetCommentToCorrectFrame(uint packetId, uint encryptionKey, uint address,
				byte[] dataOut, bool isAnswer, bool invertRequestAndAnswer)
		{
			return GetCommentToFrame(isAnswer, dataOut);
		}

		public static string GetCommentToErrorFrame(uint frameCrc, uint calculatedCrc)
		{
			if (frameCrc != calculatedCrc)
				return String.Format("Error: Frame CRC32={0:x8} != calculated CRC32={1:x8}.",
						frameCrc, calculatedCrc);
			else
				return "Error: No packet found.";
		}

		static string ReplaceCommentPattern(string s, byte[] data)
		{
			int i = 0;
			int from = 0;
			string t = "";
			while (i < s.Length)
			{
				if (s[i++] == '[')
				{
					int j = i;
					int to = i - 1;
					while (j < s.Length && s[j] != ']')
						j++;
					if (j < s.Length)
					{
						t += s[from..to];

						string u = s[i..j];
						string[] uu = u.Split(':');
						if (uu.Length >= 2 && uint.TryParse(uu[0], out uint byteNr) && byteNr < data.Length)
						{
							if (uu.Length == 2)
							{
								byte d = data[byteNr];
								if (uu[1] == "dec")
									t += d;
								else if (uu[1].Length == 4 && uu[1][..3] == "dec" &&
										int.TryParse(uu[1][3].ToString(), out int digits))
									t += d.ToString("d" + digits);
								else if (uu[1] == "hex")
									t += d.ToString("x2");
								else if (uu[1].Length == 4 && uu[1][..3] == "hex" &&
										int.TryParse(uu[1][3].ToString(), out digits))
									t += d.ToString("x" + digits);
								else if (uu[1] == "bin")
									t += Convert.ToString(d, 2).PadLeft(8, '0');
								else if (uu[1] == "rev_bin")
									t += Common.RevertString(Convert.ToString(d, 2).PadLeft(8, '0'));
								else if (uu[1] == "char")
									t += (char)d;
								else if (uu[1] == "hi_lo_16" && byteNr + 1 < data.Length)
									t += (data[byteNr] << 8) | data[byteNr + 1];
								else if (uu[1] == "lo_hi_16" && byteNr + 1 < data.Length)
									t += data[byteNr] | (data[byteNr + 1] << 8);
								else if (uu[1] == "hi_lo_24" && byteNr + 2 < data.Length)
									t += (data[byteNr] << 16) | (data[byteNr + 1] << 8) | data[byteNr + 2];
								else if (uu[1] == "lo_hi_24" && byteNr + 2 < data.Length)
									t += data[byteNr] | (data[byteNr + 1] << 8) | (data[byteNr + 2] << 16);
								else if (uu[1] == "hi_lo_32" && byteNr + 3 < data.Length)
									t += (data[byteNr] << 24) | (data[byteNr + 1] << 16) | (data[byteNr + 2] << 8) | data[byteNr + 3];
								else if (uu[1] == "lo_hi_32" && byteNr + 3 < data.Length)
									t += data[byteNr] | (data[byteNr + 1] << 8) | (data[byteNr + 2] << 16) | (data[byteNr + 3] << 24);
								else if (uu[1] == "hex_hi_lo_16" && byteNr + 1 < data.Length)
									t += "0x" + ((data[byteNr] << 8) | data[byteNr + 1]).ToString("x4");
								else if (uu[1] == "hex_lo_hi_16" && byteNr + 1 < data.Length)
									t += "0x" + (data[byteNr] | (data[byteNr + 1] << 8)).ToString("x4");
								else if (uu[1] == "hex_hi_lo_24" && byteNr + 2 < data.Length)
									t += "0x" + ((data[byteNr] << 16) | (data[byteNr + 1] << 8) | data[byteNr + 2]).ToString("x6");
								else if (uu[1] == "hex_lo_hi_24" && byteNr + 2 < data.Length)
									t += "0x" + (data[byteNr] | (data[byteNr + 1] << 8) | (data[byteNr + 2] << 16)).ToString("x6");
								else if (uu[1] == "hex_hi_lo_32" && byteNr + 3 < data.Length)
									t += "0x" + ((data[byteNr] << 24) | (data[byteNr + 1] << 16) | (data[byteNr + 2] << 8) | data[byteNr + 3]).ToString("x8");
								else if (uu[1] == "hex_lo_hi_32" && byteNr + 3 < data.Length)
									t += "0x" + (data[byteNr] | (data[byteNr + 1] << 8) | (data[byteNr + 2] << 16) | (data[byteNr + 3] << 24)).ToString("x8");
							}
							else if (uu.Length == 3)
							{
								if (uu[1] == "bool")
								{
									if (int.TryParse(uu[2], out int a) && a >= 0 && a < 8)
										t += ((data[byteNr] & (1 << a)) != 0) ? "true" : "false";
								}
								else
								{
									string[] uuu = uu[2].Split('-');
									if (uuu.Length == 2 && int.TryParse(uuu[0], out int a1) && int.TryParse(uuu[1], out int a2) &&
											a1 >= 0 && a2 >= 0 && a1 < 8 && a2 < 8 && a1 <= a2)
									{
										byte d = data[byteNr];
										d = (byte)(d >> (int)a1);
										d = (byte)(d & ((1 << (a2 - a1 + 1)) - 1));
										if (uu[1] == "dec")
											t += d;
										else if (uu[1] == "hex")
											t += d.ToString("x2");
										else if (uu[1] == "bin")
											t += Convert.ToString(d, 2).PadLeft(8, '0');
										else if (uu[1] == "rev_bin")
											t += Common.RevertString(Convert.ToString(d, 2).PadLeft(8, '0'));
										else if (uu[1] == "char")
											t += (char)d;
									}
								}
							}
							else if (uu.Length == 5 && uu[1] == "if_else") // [0:if_else:1:s1:s2]
							{
								if (int.TryParse(uu[2], out int a) && a >= 0 && a < 8)
									t += ((data[byteNr] & (1 << a)) != 0) ? uu[3] : uu[4];
							}
							else if (uu.Length == 7 && uu[1] == "if_else") // [0:if_else:7f:==:ea:s1:s2] or [0:if_else:7f:==:'a':s1:s2]
							{
								if (byte.TryParse(uu[2], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out byte mask) &&
										(byte.TryParse(uu[4], NumberStyles.HexNumber, CultureInfo.CurrentCulture, out byte equal) ||
										uu[4].Length == 3 && uu[4][0] == '\'' && uu[4][2] == '\''))
								{
									if (uu[4].Length == 3 && uu[4][0] == '\'' && uu[4][2] == '\'')
										equal = (byte)uu[4][1];
									byte d = (byte)(data[byteNr] & mask);
									if (uu[3] == "==")
										t += (d == equal) ? uu[5] : uu[6];
									else if (uu[3] == "!=")
										t += (d != equal) ? uu[5] : uu[6];
									else if (uu[3] == ">")
										t += (d > equal) ? uu[5] : uu[6];
									else if (uu[3] == "<")
										t += (d < equal) ? uu[5] : uu[6];
									else if (uu[3] == ">=")
										t += (d >= equal) ? uu[5] : uu[6];
									else if (uu[3] == "<=")
										t += (d <= equal) ? uu[5] : uu[6];
								}
							}
						}

						from = j + 1;
						i = j + 1;
					}
					else
					{
						from = i - 1;
						break;
					}
				}
			}
			t += s[from..];
			return t;
		}
	}
}

// [0:dec]                     - display byte 0
// [0:dec:0-4]                 - display byte 0 - only bits from 0 to 4
// [0:hex]                     - display byte 0 as hexadecimal
// [0:hex:4-7]                 - ...
// [0:bin]                     - display byte 0 as binary
// [0:rev_bin]                 - display byte 0 as binary - revert sequence
// [0:char]                    - display byte 0 as character
// [0:if_else:7f:==:ea:s1:s2]  - if byte 0 & 0x7f == 0xea then display 's1' otherwise display 's2'
//                                   you can use: ==, !=, >, <, >=, <=
// [0:if_else:7f:==:'a':s1:s2] - if byte 0 & 0x7f == 'a' character then display 's1' otherwise display 's2'
//                                   you can use: ==, !=, >, <, >=, <=
// [0:bool:1]                  - display byte 0 bit 1
// [0:if_else:1:s1:s2]         - if byte 0 and bit 1 set display 's1', when bit 1 is unset display 's2'
// [0:hi_lo_16]                - display 16 bits data from low byte
// [0:lo_hi_16]                - display 16 bits data from high byte
// [0:hi_lo_24]                - display 24 bits data from low byte
// [0:lo_hi_24]                - display 24 bits data from high byte
// [0:hi_lo_32]                - display 32 bits data from low byte
// [0:lo_hi_32]                - display 32 bits data from high byte
// [0:hex_hi_lo_16]            - display 16 bits data from low byte in hex
// [0:hex_lo_hi_16]            - display 16 bits data from high byte in hex
// [0:hex_hi_lo_24]            - display 24 bits data from low byte in hex
// [0:hex_lo_hi_24]            - display 24 bits data from high byte in hex
// [0:hex_hi_lo_32]            - display 32 bits data from low byte in hex
// [0:hex_lo_hi_32]            - display 32 bits data from high byte in hex
