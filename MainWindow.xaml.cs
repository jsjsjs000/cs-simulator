using InteligentnyDomSimulator.SmartHomeLibrary;
using SmartHomeTool.SmartHomeLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace InteligentnyDomSimulator
{
	public partial class MainWindow : Window
	{
		readonly string DecodePacketsConfigurationFullFilename = AppDomain.CurrentDomain.BaseDirectory +
				@"PacketsDefinition" + System.IO.Path.DirectorySeparatorChar + "DecodePackets.cfg";

		readonly ConfigurationFile cfgDecodePackets = new();

		List<CommunicationService> coms = new();
		Dictionary<uint, CheckBox[]> relaysDictionary = new();

		public MainWindow()
		{
			InitializeComponent();

			LoadDecodePacketsConfiguration();
			CommunicationService.InitializeDeviceConfiguration();

			coms.Add(new("COM6")); // COM6 COM30
			packetsLogControl.logQueues.Add(coms[0].packetsLogQueue);
			coms[0].CanLogPackets = true;
			coms[0].Connect();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			packetsLogControl.tbDebugFrames.IsChecked = false;
			CreateDevicesControls();
			DispatcherTimer dispatcherTimer = new();
			dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 300);
			dispatcherTimer.Tick += dispatcherTimer_Tick;
			dispatcherTimer.Start();
		}

		private void Window_Unloaded(object sender, RoutedEventArgs e)
		{
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			foreach (CommunicationService com in coms)
				com.ExitThread = true;
			packetsLogControl.ExitThread = true;
			//TrySaveState();
			WaitForThreads(1000);
			KillThreadsIfNotExited();
		}

		void WaitForThreads(int timeout)
		{
			DateTime start = DateTime.Now;
			while (DateTime.Now.Subtract(start).TotalMilliseconds < timeout && !AllThreadsExited())
			{
				Thread.Sleep(10);
				//Application.DoEvents();
			}
		}

		bool AllThreadsExited()
		{
			foreach (CommunicationService com in coms)
				if (!com.ExitedThread)
					return false;
			if (!packetsLogControl.ExitedThread)
				return false;
			return true;
		}

		void KillThreadsIfNotExited()
		{
			foreach (CommunicationService com in coms)
				if (!com.ExitedThread)
					com.KillThread();
			packetsLogControl.KillThread();
		}
	
		void CreateDeviceControl(DeviceItem deviceItem)
		{
			StackPanel stackPanel = new()
			{
				Orientation = Orientation.Horizontal
			};
			spDevicesList.Children.Add(stackPanel);

			TextBlock textBlockType = new()
			{
				Width = 90,
			};
			if (deviceItem.hardwareType2 == Commands.DeviceVersion.HardwareType2Enum.Temp)
				textBlockType.Text = "Temperature";
			else if (deviceItem.hardwareType2 == Commands.DeviceVersion.HardwareType2Enum.Rel)
				textBlockType.Text = "Relay";
			stackPanel.Children.Add(textBlockType);

			TextBlock textBlockAddress = new()
			{
				Text = deviceItem.address.ToString("x8") + "  (" + deviceItem.address + ")",
				Width = 150,
			};
			stackPanel.Children.Add(textBlockAddress);

			CheckBox checkBox = new()
			{
				Tag = deviceItem
			};
			checkBox.Click += (object sender, RoutedEventArgs e) =>
			{
				DeviceItem deviceItem = (DeviceItem)((FrameworkElement)sender).Tag;
				deviceItem.status!.error = (bool)((CheckBox)sender).IsChecked!;
			};
			stackPanel.Children.Add(checkBox);

			TextBlock textBlockError = new()
			{
				Text = "error",
				Width = 30,
			};
			checkBox.Content = textBlockError;

			if (deviceItem.hardwareType2 == Commands.DeviceVersion.HardwareType2Enum.Temp)
			{
				for (int i = 0; i < deviceItem.hardwareSegmentsCount; i++)
				{
					TextBox textBox = new()
					{
						Text = (deviceItem.status as TemperatureStatus)!.temperatures[i].ToString(),
						Width = 40,
						Height = 23,
						Margin = new Thickness(0, 0, 3, 3),
						Tag = new DeviceItemHandler()
						{
							status = deviceItem.status,
							index = i,
						},
					};
					textBox.TextChanged += (object sender, TextChangedEventArgs e) =>
					{
						DeviceItemHandler deviceItemHandler = (DeviceItemHandler)((FrameworkElement)sender).Tag;
						if (float.TryParse(((TextBox)sender).Text, out float temp))
							((TemperatureStatus)deviceItemHandler.status).temperatures[deviceItemHandler.index] = (ushort)temp;
					};
					stackPanel.Children.Add(textBox);
				}
			}
			else if (deviceItem.hardwareType2 == Commands.DeviceVersion.HardwareType2Enum.Rel)
			{
				relaysDictionary.Add(deviceItem.address, new CheckBox[deviceItem.hardwareSegmentsCount]);
				for (int i = 0; i < deviceItem.hardwareSegmentsCount; i++)
				{
					CheckBox checkBoxRelay = new()
					{
						IsChecked = null, // (deviceItem.status as RelayStatus)!.relays[i],
						IsEnabled = false,
						Width = 60,
						Height = 23,
						Margin = new Thickness(0, 0, 3, 3),
					};
					stackPanel.Children.Add(checkBoxRelay);
					relaysDictionary[deviceItem.address][i] = checkBoxRelay;

					TextBlock checkBoxRelayError = new()
					{
						Text = "Relay " + (i + 1),
					};
					checkBoxRelay.Content = checkBoxRelayError;
				}
			}

			TextBlock tbBlock = new()
			{
				Text = deviceItem.description,
				Margin = new Thickness(50, 0, 0, 0),
			};
			stackPanel.Children.Add(tbBlock);
		}

		void CreateDevicesControls()
		{
			foreach (DeviceItem deviceItem in CommunicationService.devicesItems.Values)
				CreateDeviceControl(deviceItem);
		}

		private void dispatcherTimer_Tick(object sender, EventArgs e)
		{
			foreach (DeviceItem deviceItem in CommunicationService.devicesItems.Values)
				if (deviceItem.hardwareType2 == Commands.DeviceVersion.HardwareType2Enum.Rel)
					for (int i = 0; i < deviceItem.hardwareSegmentsCount; i++)
						relaysDictionary[deviceItem.address][i].IsChecked = ((RelayStatus)deviceItem.status!).relays[i];
		}
	}
}

/*
	NuGet console:
	Install-Package System.Management
	Install-Package System.IO.Ports
	Install-Package SharpZipLib

*/
