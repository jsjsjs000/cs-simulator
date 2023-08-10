using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SmartHomeTool.SmartHomeLibrary;

namespace SmartHomeTool.SmartHomeWinLibrary
{
	public class WinPacketToString
	{
		readonly Color ColorIn = Colors.DarkGreen;
		readonly Color ColorOut = Colors.Blue;
		readonly Color ColorDescription = Colors.Gray;
		readonly Color ColorError = Colors.Red;
		readonly Color ColorComment = Color.FromArgb(0xff, 0xff, 0x80, 0x00);

		DateTime lastDateTime = new DateTime();

		Color selectionColor = Colors.Transparent;
		Color richTextCachedLastColor = Colors.Transparent;
		string richTextCachedText = "";

		void RichTextClearCache()
		{
			richTextCachedLastColor = Colors.Transparent;
			richTextCachedText = "";
		}

		void RichTextFlushCache(RichTextBox richText)
		{
			richText.AppendText(richTextCachedText);
			RichTextClearCache();
		}

		void RichTextAddTextCached(RichTextBox richText, string s)
		{
			TextRange rangeOfText1 = new TextRange(richText.Document.ContentEnd, richText.Document.ContentEnd);
			rangeOfText1.Text = s;
			rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(selectionColor));

			//richText.AppendText(s);
			//if (richText.SelectionColor == richTextCachedLastColor)
			//	richTextCachedText += s;
			//else
			//{
			//	Color color = richText.SelectionColor;
			//	richText.SelectionColor = richTextCachedLastColor;
			//	richText.AppendText(richTextCachedText);
			//	richTextCachedText = "";
			//	richText.SelectionColor = color;
			//	richTextCachedLastColor = color;
			//	richText.AppendText(s);
			//}
		}

		public string AddPacketText(RichTextBox richText, DateTime dt, byte[] data, Packets.PacketDirection direction,
				bool scrollToEnd)
		{
			string s = "";
			string tmp = "";
			bool lastDTok = lastDateTime.Ticks > 0;
			string lastDt = "";
			if (lastDateTime.Ticks > 0)
			{
				TimeSpan ts = dt.Subtract(lastDateTime);
				lastDt = " +";
				if (ts.Hours > 0)
					lastDt += ts.Hours.ToString() + ":";
				if (ts.Hours > 0 || ts.Minutes > 0)
				{
					if (ts.Hours > 0)
						lastDt += ts.Minutes.ToString("d2") + ":";
					else
						lastDt += ts.Minutes.ToString() + ":";
				}
				if (ts.Hours > 0 || ts.Minutes > 0)
					lastDt += ts.Seconds.ToString("d2");
				else
					lastDt += ts.Seconds.ToString();
				lastDt += "." + ts.Milliseconds.ToString("d3") + " s";
			}
			lastDateTime = dt;

			uint packetId, encryptionKey, address;
			byte[] data2;
			uint frameCrc32, calculatedCrc32;
			bool isAnswer;
			bool frameOk = Packets.FindFrameAndDecodePacketInBuffer(data, data.Length, out packetId, out encryptionKey,
					out address, out data2, out frameCrc32, out calculatedCrc32, out isAnswer);

			//richText.SuspendLayout();
			//int prevPosition = richText.SelectionStart;
			//int prevScrollLength = richText.SelectionLength;
			//richText.SelectionLength = 0;
			//richText.SelectionStart = richText.Text.Length;
			if (direction == Packets.PacketDirection.Out)
			{
				selectionColor = ColorOut;
				RichTextAddTextCached(richText, "> ");
				s += "> ";
			}
			else if (direction == Packets.PacketDirection.In)
			{
				selectionColor = ColorIn;
				RichTextAddTextCached(richText, "< ");
				s += "< ";
			}
			selectionColor = ColorDescription;
			tmp = dt.ToLongTimeString() + "." + dt.Millisecond.ToString("d3") + lastDt + " - " + data.Length + " bytes";
			RichTextAddTextCached(richText, tmp);
			s += tmp;
			if (direction == Packets.PacketDirection.Out)
				selectionColor = ColorOut;
			else if (direction == Packets.PacketDirection.In)
				selectionColor = ColorIn;

			int lastIChar = 0;
			for (int i = 0; i < data.Length; i++)
			{
				if (i % 16 == 0)
				{
					RichTextAddTextCached(richText, Environment.NewLine);
					s += Environment.NewLine;
					if (direction == Packets.PacketDirection.Out)
						selectionColor = ColorOut;
					else if (direction == Packets.PacketDirection.In)
						selectionColor = ColorIn;
					RichTextAddTextCached(richText, "  ");
					s += "  ";
				}
				tmp = data[i].ToString("x2") + " ";
				if (i >= Packets.PacketPreBytes && i < data.Length - Packets.PacketPostBytes)
				{
					if (direction == Packets.PacketDirection.Out)
						selectionColor = ColorOut;
					else if (direction == Packets.PacketDirection.In)
						selectionColor = ColorIn;
				}
				else
					selectionColor = ColorDescription;
				RichTextAddTextCached(richText, tmp);
				s += tmp;
				if (i % 4 == 4 - 1)
				{
					RichTextAddTextCached(richText, " ");
					s += " ";
				}
				if (i % 16 == 16 - 1)
				{
					selectionColor = ColorDescription;
					RichTextAddTextCached(richText, " [");
					s += " [";
					for (int j = i - 15; j <= i; j++)
					{
						if (j >= Packets.PacketPreBytes && j < data.Length - Packets.PacketPostBytes)
						{
							if (direction == Packets.PacketDirection.Out)
								selectionColor = ColorOut;
							else if (direction == Packets.PacketDirection.In)
								selectionColor = ColorIn;
						}
						if (data[j] >= 32 && data[j] <= 126)
						{
							tmp = ((char)data[j]).ToString();
							RichTextAddTextCached(richText, tmp);
							s += tmp;
						}
						else
						{
							RichTextAddTextCached(richText, ".");
							s += ".";
						}
					}
					RichTextAddTextCached(richText, "]");
					s += "]";
					lastIChar = i;
				}
			}

			if (data.Length % 16 != 0)
			{
				var to = data.Length / 16 * 16 + 16;
				for (int j = data.Length; j < to; j++)
				{
					if (j % 4 == 4 - 1)
					{
						RichTextAddTextCached(richText, " ");
						s += " ";
					}
					RichTextAddTextCached(richText, "   ");
					s += "   ";
				}

				RichTextAddTextCached(richText, " [");
				s += " [";
				if (lastIChar == 0)
					lastIChar--;
				for (int j = lastIChar + 1; j < data.Length; j++)
				{
					if (j >= Packets.PacketPreBytes && j < data.Length - Packets.PacketPostBytes)
					{
						if (direction == Packets.PacketDirection.Out)
							selectionColor = ColorOut;
						else if (direction == Packets.PacketDirection.In)
							selectionColor = ColorIn;
					}
					else
						selectionColor = ColorDescription;
					if (data[j] >= 32 && data[j] <= 126)
					{
						tmp = ((char)data[j]).ToString();
						RichTextAddTextCached(richText, tmp);
						s += tmp;
					}
					else
					{
						RichTextAddTextCached(richText, ".");
						s += ".";
					}
				}

				RichTextAddTextCached(richText, "]");
				s += "]";

				for (int j = data.Length; j < to; j++)
				{
					RichTextAddTextCached(richText, " ");
					s += " ";
				}
			}

			if (frameOk)
			{
				selectionColor = ColorDescription;
				tmp = " frame ok";
				RichTextAddTextCached(richText, tmp);
				s += tmp;
			}
			else
			{
				selectionColor = ColorError;
				if (frameCrc32 != calculatedCrc32)
				{
					tmp = " frame CRC32=" + frameCrc32.ToString("x8") + " != calculated=" + calculatedCrc32.ToString("x8");
					RichTextAddTextCached(richText, tmp);
					s += tmp;
				}
				else
				{
					tmp = " frame NOT OK";
					RichTextAddTextCached(richText, tmp);
					s += tmp;
				}
			}

			RichTextAddTextCached(richText, Environment.NewLine);
			s += Environment.NewLine;
			RichTextFlushCache(richText);
			if (scrollToEnd)
				richText.ScrollToEnd();
			else
			{
				//richText.SelectionStart = prevPosition;
				//richText.SelectionLength = prevScrollLength;
			}
			//selectionColor = SystemColors.WindowText;
			//richText.ResumeLayout();
			return s;
		}

		public string GetPacketText(DateTime dt, byte[] data, Packets.PacketDirection direction)
		{
			string s = "";
			bool lastDTok = lastDateTime.Ticks > 0;
			string lastDt = "";
			if (lastDateTime.Ticks > 0)
			{
				TimeSpan ts = dt.Subtract(lastDateTime);
				lastDt = " +";
				if (ts.Hours > 0)
					lastDt += ts.Hours.ToString() + ":";
				if (ts.Hours > 0 || ts.Minutes > 0)
				{
					if (ts.Hours > 0)
						lastDt += ts.Minutes.ToString("d2") + ":";
					else
						lastDt += ts.Minutes.ToString() + ":";
				}
				if (ts.Hours > 0 || ts.Minutes > 0)
					lastDt += ts.Seconds.ToString("d2");
				else
					lastDt += ts.Seconds.ToString();
				lastDt += "." + ts.Milliseconds.ToString("d3") + " s";
			}
			lastDateTime = dt;

			uint packetId, encryptionKey, address;
			byte[] data2;
			uint frameCrc32, calculatedCrc32;
			bool isAnswer;
			bool frameOk = Packets.FindFrameAndDecodePacketInBuffer(data, data.Length, out packetId, out encryptionKey,
					out address, out data2, out frameCrc32, out calculatedCrc32, out isAnswer);

			if (direction == Packets.PacketDirection.Out)
				s += "> ";
			else if (direction == Packets.PacketDirection.In)
				s += "< ";
			s += dt.ToLongTimeString() + "." + dt.Millisecond.ToString("d3") + lastDt + " - " + data.Length + " bytes";

			int lastIChar = 0;
			for (int i = 0; i < data.Length; i++)
			{
				if (i % 16 == 0)
					s += Environment.NewLine + "  ";
				s += data[i].ToString("x2") + " ";
				if (i % 4 == 4 - 1)
					s += " ";
				if (i % 16 == 16 - 1)
				{
					s += " [";
					for (int j = i - 15; j <= i; j++)
						if (data[j] >= 32 && data[j] <= 126)
							s += ((char)data[j]).ToString();
						else
							s += ".";
					s += "]";
					lastIChar = i;
				}
			}

			if (data.Length % 16 != 0)
			{
				var to = data.Length / 16 * 16 + 16;
				for (int j = data.Length; j < to; j++)
				{
					if (j % 4 == 4 - 1)
						s += " ";
					s += "   ";
				}

				s += " [";
				if (lastIChar == 0)
					lastIChar--;
				for (int j = lastIChar + 1; j < data.Length; j++)
					if (data[j] >= 32 && data[j] <= 126)
						s += ((char)data[j]).ToString();
					else
						s += ".";

				s += "]";

				for (int j = data.Length; j < to; j++)
					s += " ";
			}

			if (frameOk)
				s += " frame ok";
			else
			{
				if (frameCrc32 != calculatedCrc32)
					s += " frame CRC32=" + frameCrc32.ToString("x8") + " != calculated=" + calculatedCrc32.ToString("x8");
				else
					s += " frame NOT OK";
			}

			s += Environment.NewLine;
			return s;
		}

		public void AddText(RichTextBox richText, string s, bool scrollToEnd)
		{
			selectionColor = SystemColors.WindowTextColor;
			RichTextAddTextCached(richText, s);

			if (scrollToEnd)
				richText.ScrollToEnd();
			RichTextFlushCache(richText);
		}

		public void AddDebugText(RichTextBox richText, string s, bool isError, bool scrollToEnd)
		{
			//richText.SuspendLayout();
			//int prevPosition = richText.SelectionStart;
			//int prevScrollLength = richText.SelectionLength;
			//richText.SelectionLength = 0;
			//richText.SelectionStart = richText.Text.Length;

			if (isError)
				selectionColor = ColorError;
			else
				selectionColor = ColorComment;
			RichTextAddTextCached(richText, s + Environment.NewLine);

			if (scrollToEnd)
				richText.ScrollToEnd();
			else
			{
				//richText.SelectionStart = prevPosition;
				//richText.SelectionLength = prevScrollLength;
			}
			RichTextFlushCache(richText);
			//selectionColor = SystemColors.WindowText;
			//richText.ResumeLayout();
		}

		public void AddColoredDebugText(RichTextBox richText, string s)
		{
			string[] lines = s.Split(new string[] { "{$" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string line in lines)
			{
				int i = line.IndexOf('}');
				if (i >= 0)
				{
					string code = line.Substring(0, i);
					if (code == "red")
						selectionColor = Colors.Red;
					else if (code == "blue")
						selectionColor = Colors.Blue;
					else if (code == "green")
						selectionColor = Colors.DarkGreen;
					else if (code == "yellow" || code == "orange")
						selectionColor = Colors.DarkOrange;
					else if (code == "default")
						selectionColor = SystemColors.WindowTextColor;
					richText.AppendText(line.Substring(i + 1, line.Length - i - 1));
				}
				else
					richText.AppendText(line);
			}
		}
	}
}
