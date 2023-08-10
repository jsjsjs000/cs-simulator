using SmartHomeTool.SmartHomeLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InteligentnyDomSimulator
{
	partial class MainWindow
	{
		void LoadDecodePacketsConfiguration()
		{
			PacketsComments.PacketDecodeAndCommentItems.Clear();
			string error = cfgDecodePackets.ReadConfigurationFromFile(DecodePacketsConfigurationFullFilename, false);
			if (error.Length > 0)
			{
				_ = MessageBox.Show("Can't load configuration file:" + Environment.NewLine + // $$ lang
						"'" + DecodePacketsConfigurationFullFilename + "'." + Environment.NewLine + Environment.NewLine + error,
						App.ProgramName, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			int i = 1;
			while (true)
			{
				if (!cfgDecodePackets.ReadString("DecodePackets", "Item" + i, out string s))
					break;
				else
				{
					PacketDecodeAndCommentItem? packetDecodeAndCommentItem = PacketDecodeAndCommentItem.ParseFromString(s, out error);
					if (error.Length > 0)
					{
						_ = MessageBox.Show("Can't load configuration file '" + DecodePacketsConfigurationFullFilename + "'." +
								Environment.NewLine + Environment.NewLine + error,
								App.ProgramName, MessageBoxButton.OK, MessageBoxImage.Error); // $$ lang
						return;
					}
					if (packetDecodeAndCommentItem != null)
						PacketsComments.PacketDecodeAndCommentItems.Add(packetDecodeAndCommentItem);
				}
				i++;
			}
		}
	}
}
