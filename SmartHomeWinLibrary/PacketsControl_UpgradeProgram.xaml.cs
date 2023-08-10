using Microsoft.Win32;
using SmartHomeTool.SmartHomeLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartHomeTool.SmartHomeWinLibrary
{
	public partial class PacketsControl : UserControl
	{
		private void TbUpgradeProgram_TextChanged(object sender, RoutedEventArgs e)
		{
			if (!areControlsInitialized)
				return;

			cfg.SetValue("UpgradeProgram", "PacketId", tbUpgradeProgramPacketId.Text);
			cfg.SetValue("UpgradeProgram", "EncryptionKey", tbUpgradeProgramEncryptionKey.Text);
			cfg.SetValue("UpgradeProgram", "Address", tbUpgradeProgramAddress.Text);
			cfg.SetValue("UpgradeProgram", "PacketsFrom", tbUpgradeProgramPacketsFrom.Text);
			cfg.SetValue("UpgradeProgram", "PacketsTo", tbUpgradeProgramPacketsTo.Text);
			WriteConfiguration();
			UpgradeProgramGetParameters(out _, out _, out _);
		}

		private void TbUpgradeProgramPathEdit_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!areControlsInitialized)
				return;

			cfg.SetValue("UpgradeProgram", "ProgramPath", tbUpgradeProgramPathEdit.Text);
			WriteConfiguration();

			LoadProgram(false);
		}

		private void BtnOpenProgram_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new();
			if (Directory.Exists(Path.GetDirectoryName(tbUpgradeProgramPathEdit.Text)))
				openFileDialog.InitialDirectory = Path.GetDirectoryName(tbUpgradeProgramPathEdit.Text);
			openFileDialog.Filter = "Bin file (*.bin)|*bin";
			if (openFileDialog.ShowDialog() != true)
				return;

			ClearProgram();
			tbUpgradeProgramPathEdit.Text = openFileDialog.FileName;
			LoadProgram(true);
		}

		void ClearProgram()
		{
			upgradeProgram.loadedProgramPath = "";
			upgradeProgram.loadedProgramData = Array.Empty<byte>();
			upgradeProgram.loadedProgramDeviceClass = new DeviceClass();
			upgradeProgram.deviceVersion = null;
			tbUpgradeProgramError.Text = "Program not load";
			tbUpgradeProgramError.Foreground = Brushes.Red;
		}

		private void LoadProgram(bool canShowDialogWindow)
		{
			ClearProgram();

			try
			{
				if (!File.Exists(tbUpgradeProgramPathEdit.Text))
					return;

				upgradeProgram.loadedProgramData = File.ReadAllBytes(tbUpgradeProgramPathEdit.Text);
				upgradeProgram.modifyDateTime = File.GetLastWriteTime(tbUpgradeProgramPathEdit.Text);
				upgradeProgram.deviceVersion = UpgradeProgram.DecodeProgramFromBinary(upgradeProgram.loadedProgramData);
				if (upgradeProgram.deviceVersion == null)
				{
					if (/*this.Visible && */canShowDialogWindow)
						MessageBox.Show("This is not valid binary program.",
								App.ProgramName, MessageBoxButton.OK, MessageBoxImage.Error); // $$ lang
					return;
				}

				upgradeProgram.loadedProgramPath = tbUpgradeProgramPathEdit.Text;
				tbUpgradeProgramError.Text = upgradeProgram.VersionToString();
				tbUpgradeProgramError.Foreground = Brushes.DarkGreen;
			}
			catch (Exception ex)
			{
				ClearProgram();
				if (/*this.Visible && */canShowDialogWindow)
					MessageBox.Show("Can't load file '" + tbUpgradeProgramPathEdit.Text + "'." +
							Environment.NewLine + Environment.NewLine + ex.ToString(),
							App.ProgramName, MessageBoxButton.OK, MessageBoxImage.Error); // $$ lang
			}
		}

		private bool UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address)
		{
			encryptionKey = 0;
			address = 0;

			if (!uint.TryParse(tbUpgradeProgramPacketId.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out packetId))
			{
				tbUpgradeProgramPacketId.Foreground = Brushes.Red;
				return false;
			}
			tbUpgradeProgramPacketId.Foreground = SystemColors.WindowTextBrush;

			if (!uint.TryParse(tbUpgradeProgramEncryptionKey.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out encryptionKey))
			{
				tbUpgradeProgramEncryptionKey.Foreground = Brushes.Red;
				return false;
			}
			tbUpgradeProgramEncryptionKey.Foreground = SystemColors.WindowTextBrush;

			if (!uint.TryParse(tbUpgradeProgramAddress.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out address))
			{
				tbUpgradeProgramAddress.Foreground = Brushes.Red;
				return false;
			}
			tbUpgradeProgramAddress.Foreground = SystemColors.WindowTextBrush;

			return true;
		}

		bool HardwareVersionGetParameters(out Commands.DeviceVersion.HardwareType1Enum hardwareType1,
				out Commands.DeviceVersion.HardwareType2Enum hardwareType2, out byte hardwareTypeCount, out byte hardwareVersion)
		{
			hardwareType1 = Commands.DeviceVersion.HardwareType1Enum.None;
			hardwareType2 = Commands.DeviceVersion.HardwareType2Enum.None;
			hardwareTypeCount = 0;
			hardwareVersion = 0;

			if (!byte.TryParse(tbFbHardwareType1.Text, out byte hardwareType1_))
			{
				tbFbHardwareType1.Foreground = Brushes.Red;
				return false;
			}
			tbFbHardwareType1.Foreground = SystemColors.WindowTextBrush;
			hardwareType1 = (Commands.DeviceVersion.HardwareType1Enum)hardwareType1_;

			if (!byte.TryParse(tbFbHardwareType2.Text, out byte hardwareType2_))
			{
				tbFbHardwareType2.Foreground = Brushes.Red;
				return false;
			}
			tbFbHardwareType2.Foreground = SystemColors.WindowTextBrush;
			hardwareType2 = (Commands.DeviceVersion.HardwareType2Enum)hardwareType2_;

			if (!byte.TryParse(tbFbHardwareTypeCount.Text, out hardwareTypeCount))
			{
				tbFbHardwareTypeCount.Foreground = Brushes.Red;
				return false;
			}
			tbFbHardwareTypeCount.Foreground = SystemColors.WindowTextBrush;

			if (!byte.TryParse(tbFbHardwareVersion.Text, out hardwareVersion))
			{
				tbFbHardwareVersion.Foreground = Brushes.Red;
				return false;
			}
			tbFbHardwareVersion.Foreground = SystemColors.WindowTextBrush;

			return true;
		}

		private void BtnUpgradeProgramSendF0_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			_ = com.cmd.SendBootloader_ClearDeviceProgram(packetId, encryptionKey, address);
		}

		private void BtnUpgradeProgramSendF1_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			ushort packetsCount = upgradeProgram.GetPacketsCount();
			_ = com.cmd.SendBootloader_ClearDeviceMemory(packetId, encryptionKey, address, packetsCount, DateTime.Now,
					out _, out _);
		}

		private void BtnUpgradeProgramSendF2_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			_ = com.cmd.SendBootloader_CheckIsClearDeviceMemory(packetId, encryptionKey, address, out _);
		}

		private void BtnUpgradeProgramSendF3_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			if (!ushort.TryParse(tbUpgradeProgramPacketsFrom.Text, out ushort packetsFrom) ||
					!ushort.TryParse(tbUpgradeProgramPacketsTo.Text, out ushort packetsTo))
				return;

			ushort packetsCount = upgradeProgram.GetPacketsCount();
			packetsTo = (ushort)Math.Min(packetsTo, packetsCount - 1);

			bool canLogPackets = com.CanLogPackets;
			com.CanLogPackets = false;
			for (ushort i = packetsFrom; i <= packetsTo; i++)
			{
				byte[] packet = upgradeProgram.GetPacket(i);
				com.cmd.SendBootloader_SendPacketToDevice(packetId, encryptionKey, address, i, packet);

				lock (com.packetsLogQueue)
					com.packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Debug, DateTime.Now, Array.Empty<byte>(),
							Packets.PacketDirection.Out, $"Send packet {i}"));
			}
			com.CanLogPackets = canLogPackets;
		}

		private void BtnUpgradeProgramSendF4_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			_ = com.cmd.SendBootloader_GetNotResolvedPacketsList(packetId, encryptionKey, address, out _);
		}

		private void BtnUpgradeProgramSendF5_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			uint crc32 = Crc32.CalculateCrc32(0, upgradeProgram.loadedProgramData);
			_ = com.cmd.SendBootloader_EndDeviceProgramming(packetId, encryptionKey, address, crc32, out _);
		}

		private void BtnGetDeviceAddressFA_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			string type = (string)((Button)sender).Tag;
			byte type_ = byte.Parse(type);
			_ = com.cmd.SendGetDeviceAddress(packetId, encryptionKey, address, type_, out _);
		}

		private void TbFbAddress_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!areControlsInitialized)
				return;

			if (!uint.TryParse(tbFbAddress.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out _))
			{
				tbFbAddress.Foreground = Brushes.Red;
				return;
			}
			tbFbAddress.Foreground = SystemColors.WindowTextBrush;

			cfg.SetValue("UpgradeProgram", "FbAddress", tbFbAddress.Text);
			WriteConfiguration();
		}

		void TbFbHardware_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!areControlsInitialized)
				return;

			if (!HardwareVersionGetParameters(out Commands.DeviceVersion.HardwareType1Enum hardwareType1,
					out Commands.DeviceVersion.HardwareType2Enum hardwareType2,
					out byte hardwareTypeCount, out byte hardwareVersion))
				return;

			cfg.SetValue("UpgradeProgram", "HardwareType1", ((byte)hardwareType1).ToString());
			cfg.SetValue("UpgradeProgram", "HardwareType2", ((byte)hardwareType2).ToString());
			cfg.SetValue("UpgradeProgram", "HardwareTypeCount", hardwareTypeCount.ToString());
			cfg.SetValue("UpgradeProgram", "HardwareVersion", hardwareVersion.ToString());
			WriteConfiguration();
		}

		private void BtnSetDeviceAddressF9_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			if (!HardwareVersionGetParameters(out Commands.DeviceVersion.HardwareType1Enum hardwareType1,
					out Commands.DeviceVersion.HardwareType2Enum hardwareType2,
					out byte hardwareTypeCount, out byte hardwareVersion))
				return;

			if (!uint.TryParse(tbFbAddress.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint newAddress))
			{
				tbFbAddress.Foreground = Brushes.Red;
				return;
			}
			tbFbAddress.Foreground = SystemColors.WindowTextBrush;

			_ = com.cmd.SendBootloader_SetHardwareIdAndDeviceAddress(packetId, encryptionKey, address,
					hardwareType1, hardwareType2, hardwareTypeCount, hardwareVersion, newAddress, out _);
		}

		private void TbFcAddress_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!areControlsInitialized)
				return;

			if (!uint.TryParse(tbFcAddress.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out _))
			{
				tbFcAddress.Foreground = Brushes.Red;
				return;
			}
			tbFcAddress.Foreground = SystemColors.WindowTextBrush;

			cfg.SetValue("UpgradeProgram", "FcAddress", tbFcAddress.Text);
			WriteConfiguration();
		}

		private void TbFcLength_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!areControlsInitialized)
				return;

			if (!ushort.TryParse(tbFcLength.Text, out ushort length) || length == 0 || length > 256)
			{
				tbFcLength.Foreground = Brushes.Red;
				return;
			}
			tbFcLength.Foreground = SystemColors.WindowTextBrush;

			cfg.SetValue("UpgradeProgram", "FcLength", tbFcLength.Text);
			WriteConfiguration();
		}

		private void BtnGetFlashMemoryFC_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			if (!uint.TryParse(tbFcAddress.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint flashAddress))
				return;

			if (!ushort.TryParse(tbFcLength.Text, out ushort length) || length == 0 || length > 256)
				return;

			_ = com.cmd.SendGetFlashMemory(packetId, encryptionKey, address, flashAddress, length, out byte[] data);
			string s = Common.ArrayBytesToHexStringWithAddress(data, flashAddress);
			//com.packetsLogQueue.Enqueue(new PacketLog());
			//packetsLogControl.getRichText.AppendText(s + Environment.NewLine + Environment.NewLine);
			packetsLogControl.winPacketToString.AddText(packetsLogControl.getRichText,
					s + Environment.NewLine + Environment.NewLine, true);
		}

		private void BtnReset_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			_ = com.cmd.SendReset(packetId, encryptionKey, address, 0, out _);
		}

		void BtnVersion_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			_ = com.cmd.SendGetDeviceVersion(packetId, encryptionKey, address, out _);
		}

		private void BtnSynchronize_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			com.cmd.SendSynchronize(packetId, encryptionKey, address);
		}

		private void BtnSynchronizeRandomTime_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			Random rnd = new();
			DateTime dt = new(2010 + rnd.Next(12), 1 + rnd.Next(11), 1 + rnd.Next(28),
					rnd.Next(23), rnd.Next(59), rnd.Next(59), rnd.Next(999));
			com.cmd.SendSynchronize(packetId, encryptionKey, address, dt);
		}

		private void TbFbLine_TextChanged(object sender, RoutedEventArgs e)
		{
			if (!areControlsInitialized)
				return;

			if (!byte.TryParse(tbFbLine.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out _))
			{
				tbFcAddress.Foreground = Brushes.Red;
				return;
			}
			tbFbLine.Foreground = SystemColors.WindowTextBrush;

			cfg.SetValue("UpgradeProgram", "FbLine", tbFbLine.Text);
			WriteConfiguration();
		}

		private void BtnDirectModeOn_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			if (!byte.TryParse(tbFbLine.Text, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out byte line))
				return;

			_ = com.cmd.SendDirectMode(packetId, encryptionKey, address, line);
		}

		private void BtnDirectModeOff_Click(object sender, RoutedEventArgs e)
		{
			if (!UpgradeProgramGetParameters(out uint packetId, out uint encryptionKey, out uint address))
				return;

			_ = com.cmd.SendDirectMode(packetId, encryptionKey, address, 0);
		}
	}
}
