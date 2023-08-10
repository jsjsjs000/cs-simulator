using SmartHomeTool.SmartHomeLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SmartHomeTool.SmartHomeWinLibrary
{
	public partial class PacketsControl : UserControl
	{
		readonly UpgradeProgram upgradeProgram = new();
		public CommunicationService com;
		public ConfigurationFile cfg;
		public PacketsLogControl packetsLogControl;
		bool areControlsInitialized;

		public delegate void WriteConfigurationDelegate();
		public WriteConfigurationDelegate writeConfiguration;

		public PacketsControl()
		{
			InitializeComponent();
		}

		private void Control_Loaded(object sender, RoutedEventArgs e)
		{
			CheckPacketControls(out _);
			TbPacketDecode_TextChanged(null, null);
			TbUpgradeProgramPathEdit_TextChanged(null, null);
			areControlsInitialized = true;
		}

		public void LoadConfiguration(ConfigurationFile cfg)
		{
			if (cfg.ReadString("Packets", "PacketId", out string pid))
				tbPacketsPacketId.Text = pid;
			if (cfg.ReadString("Packets", "EncryptionKey", out string ek))
				tbPacketsEncryptionKey.Text = ek;
			if (cfg.ReadString("Packets", "Address", out string adr))
				tbPacketsAddress.Text = adr;
			if (cfg.ReadString("Packets", "Frame", out string f))
				tbPacketsEdit.Text = f;
			if (cfg.ReadString("Packets", "DecodePacket", out string dp))
				tbPacketDecodeEdit.Text = dp;

			if (cfg.ReadString("UpgradeProgram", "PacketId", out string pid_))
				tbUpgradeProgramPacketId.Text = pid_;
			if (cfg.ReadString("UpgradeProgram", "EncryptionKey", out string ek_))
				tbUpgradeProgramEncryptionKey.Text = ek_;
			if (cfg.ReadString("UpgradeProgram", "Address", out string adr_))
				tbUpgradeProgramAddress.Text = adr_;
			if (cfg.ReadString("UpgradeProgram", "PacketsFrom", out string packetsFrom_))
				tbUpgradeProgramPacketsFrom.Text = packetsFrom_;
			if (cfg.ReadString("UpgradeProgram", "PacketsTo", out string packetsTo_))
				tbUpgradeProgramPacketsTo.Text = packetsTo_;
			if (cfg.ReadString("UpgradeProgram", "ProgramPath", out string pp))
				tbUpgradeProgramPathEdit.Text = pp;
			if (cfg.ReadString("UpgradeProgram", "FbAddress", out string fb))
				tbFbAddress.Text = fb;
			if (cfg.ReadString("UpgradeProgram", "FcAddress", out string fc))
				tbFcAddress.Text = fc;
			if (cfg.ReadString("UpgradeProgram", "FcLength", out string fcl))
				tbFcLength.Text = fcl;
			if (cfg.ReadString("UpgradeProgram", "HardwareType1", out string ht1))
				tbFbHardwareType1.Text = ht1;
			if (cfg.ReadString("UpgradeProgram", "HardwareType2", out string ht2))
				tbFbHardwareType2.Text = ht2;
			if (cfg.ReadString("UpgradeProgram", "HardwareTypeCount", out string htc))
				tbFbHardwareTypeCount.Text = htc;
			if (cfg.ReadString("UpgradeProgram", "HardwareVersion", out string hv))
				tbFbHardwareVersion.Text = hv;
			if (cfg.ReadString("UpgradeProgram", "FbLine", out string fb2))
				tbFbLine.Text = fb2;

			int i = 0;
			tbPacketsEdit.Items.Clear();
			while (cfg.ReadString("Packets", $"Frame_{i++}", out string s))
				tbPacketsEdit.Items.Add(s);
		}

		public void WriteConfiguration()
		{
			writeConfiguration();
		}

		private bool CheckPacketControls(out byte[] data2)
		{
			tbPacketsSop.Text = "a0  00 00";
			tbPacketsEop.Text = "00 00 00 00  a1";
			tbPacketsPacketId.Foreground = SystemColors.WindowTextBrush;
			tbPacketsEncryptionKey.Foreground = SystemColors.WindowTextBrush;
			tbPacketsAddress.Foreground = SystemColors.WindowTextBrush;
			tbPacketsPacketError.Foreground = SystemColors.WindowTextBrush;
			tbPacketsPacketError.Text = "";
			data2 = Array.Empty<byte>();

			if (!uint.TryParse(tbPacketsPacketId.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint packetId))
			{
				tbPacketsPacketId.Foreground = Brushes.Red;
				tbPacketsPacketError.Text = "Error: Can't parse hex number Packet Id"; // $$ lang
				tbPacketsPacketError.Foreground = Brushes.Red;
				return false;
			}

			if (!uint.TryParse(tbPacketsEncryptionKey.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint encryptionKey))
			{
				tbPacketsEncryptionKey.Foreground = Brushes.Red;
				tbPacketsPacketError.Text = "Error: Can't parse hex number Encryption Key"; // $$ lang
				tbPacketsPacketError.Foreground = Brushes.Red;
				return false;
			}

			if (!uint.TryParse(tbPacketsAddress.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint address))
			{
				tbPacketsAddress.Foreground = Brushes.Red;
				tbPacketsPacketError.Text = "Error: Can't parse hex number Address"; // $$ lang
				tbPacketsPacketError.Foreground = Brushes.Red;
				return false;
			}

			string error = ParsePacket.ParsePacketFromString(tbPacketsEdit.Text, out byte[] data);
			if (error.Length > 0)
			{
				tbPacketsPacketError.Text = error;
				tbPacketsPacketError.Foreground = Brushes.Red;
				return false;
			}

			data2 = Packets.EncodePacket(packetId, encryptionKey, address, data, false);
			if (data2.Length > 15 + 5)
			{
				tbPacketsSop.Text = $"{data2[0]:x2}  {data2[1]:x2} {data2[2]:x2}";
				tbPacketsEop.Text = $"{data2[^5]:x2} {data2[^4]:x2} " +
						$"{data2[^3]:x2} {data2[^2]:x2}  {data2[^1]:x2}";
			}

			tbPacketsPacketError.Text = PacketsComments.GetCommentToFrame(false, data);

			return true;
		}

		private void TbPackets_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!areControlsInitialized)
				return;
			cfg.SetValue("Packets", "PacketId", tbPacketsPacketId.Text);
			cfg.SetValue("Packets", "EncryptionKey", tbPacketsEncryptionKey.Text);
			cfg.SetValue("Packets", "Address", tbPacketsAddress.Text);
			WriteConfiguration();
			CheckPacketControls(out _);
		}

		private void TbPacketsEdit_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!areControlsInitialized)
				return;
			WritePacketsToConfiguration();
			CheckPacketControls(out _);
		}

		private void TbPacketsEdit_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!areControlsInitialized)
				return;
			WritePacketsToConfiguration();
			CheckPacketControls(out _);
		}

		void AddPacketToComboBox(string s)
		{
			areControlsInitialized = false;
			if (tbPacketsEdit.Items.Contains(s.Trim()))
				tbPacketsEdit.Items.Remove(s.Trim());
			tbPacketsEdit.Items.Insert(0, s.Trim());
			tbPacketsEdit.Text = s.Trim();
			areControlsInitialized = true;
		}

		private void TbPacketsEdit_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				BtnPacketSend_Click(null, null);
		}

		void BtnRemovePacketFromComboBox(object sender, RoutedEventArgs e)
		{
			tbPacketsEdit.Items.Remove(tbPacketsEdit.Text);
			tbPacketsEdit.Text = "";
		}

		void WritePacketsToConfiguration()
		{
			cfg.SetValue("Packets", "Frame", tbPacketsEdit.Text);
			int i;
			for (i = 0; i < tbPacketsEdit.Items.Count; i++)
				cfg.SetValue("Packets", $"Frame_{i}", (string)tbPacketsEdit.Items[i]);
			cfg.DeleteKey("Packets", $"Frame_{i}");
			WriteConfiguration();
		}

		private void BtnPacketSend_Click(object sender, RoutedEventArgs e)
		{
			if (!CheckPacketControls(out byte[] data2))
				return;

			if (!com.IsConnected())
				return;

			AddPacketToComboBox(tbPacketsEdit.Text);
			WritePacketsToConfiguration();
			com.Send(data2, out _, out _, out _, out _);
		}

		private void TbPacketDecode_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!areControlsInitialized)
				return;

			cfg.SetValue("Packets", "DecodePacket", tbPacketDecodeEdit.Text);
			WriteConfiguration();

			tbPacketsDecodeError.Text = "";
			tbPacketsDecodeError.Foreground = SystemColors.WindowTextBrush;

			string error = ParsePacket.ParsePacketFromString(tbPacketDecodeEdit.Text, out byte[] data);
			if (error.Length > 0)
			{
				tbPacketsDecodeError.Text = error;
				tbPacketsDecodeError.Foreground = Brushes.Red;
				return;
			}

			if (!Packets.FindFrameAndDecodePacketInBuffer(data, data.Length, out _, out _, out _,
					out byte[] data2, out uint frameCrc32, out uint calculatedCrc32, out bool isAnswer))
			{
				tbPacketsDecodeError.Foreground = Brushes.Red;
				if (frameCrc32 != calculatedCrc32)
					tbPacketsDecodeError.Text =
							$"Error: Frame CRC32={frameCrc32:x8} != calculated CRC32={calculatedCrc32:x8}";
				else
					tbPacketsDecodeError.Text = "Error: No packet found.";
				return;
			}

			tbPacketsDecodeError.Text = PacketsComments.GetCommentToFrame(isAnswer, data2);
		}
	}
}
