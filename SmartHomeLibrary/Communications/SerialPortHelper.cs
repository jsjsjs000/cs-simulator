using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Management;

namespace SmartHomeTool.SmartHomeLibrary
{
	class SerialPortHelper
	{
		private const string QueryString = "SELECT * FROM MSSerial_PortName";
		private const string QueryString1 = @"SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0 AND Caption like '%(COM%'";

		public static Dictionary<string, string> GetWindowsComFriendlyNames()
		{
			Dictionary<string, string> names = new();
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				foreach (string name in Directory.GetFiles("/dev/", "ttyUSB*"))
					names.Add(name, "");
				foreach (string name in Directory.GetFiles("/dev/", "ttyS*"))
					names.Add(name, "");
				return names;
			}
			else
				try
				{
					/// PowerShell: Get-WmiObject -namespace 'root\WMI' -class 'MSSerial_PortName'
#pragma warning disable CA1416 // Walidacja zgodności z platformą
					Dictionary<string, string> list = new();
					ManagementObjectSearcher searcher = new("root\\WMI", QueryString);
					string[] USBPorts = System.IO.Ports.SerialPort.GetPortNames();
					foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
						if (obj != null && obj["InstanceName"] is string && obj["PortName"] is string)
							list.Add(((string)obj["InstanceName"]).ToLower(), (string)obj["PortName"]);

					/// PowerShell: Get-WMIObject Win32_PnPEntity -Filter "Caption like '%(COM%'"
					searcher = new ManagementObjectSearcher(QueryString1);
					foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
						if (obj != null)
						{
							object DeviceID = obj["DeviceID"];
							object Name = obj["Name"];
							if (DeviceID is string && Name is string)
								for (int i = 0; i < 20; i++)
									if (list.ContainsKey(((string)DeviceID + "_" + i).ToLower()))
									{
										names.Add(list[((string)DeviceID + "_" + i).ToLower()], (string)Name);
										break;
									}
						}
				}
				catch (ManagementException) { }
#pragma warning restore CA1416 // Walidacja zgodności z platformą

			/// Sort names
			SortedDictionary<int, string[]> sort = new();
			foreach (string name in names.Keys)
			{
				string s = "";
				foreach (char c in name)
					if (c >= '0' && c <= '9')
						s += c;
				if (int.TryParse(s, out int n))
					sort.Add(n, new string[] { name, names[name] });
			}
			names = new Dictionary<string, string>();
			foreach (int key in sort.Keys)
				names.Add(sort[key][0], sort[key][1]);
			return names;
		}

		public static int GetComTimeoutInRegistry(string comName)
		{
			try
			{
#pragma warning disable CA1416 // Walidacja zgodności z platformą
				string path = @"SYSTEM\ControlSet001\Enum\FTDIBUS";
				RegistryKey? rk = Registry.LocalMachine.OpenSubKey(path);
				string[]? subkeys = rk?.GetSubKeyNames();
				rk?.Close();
				foreach (string subkey2 in subkeys)
				{
					RegistryKey? rk2 = Registry.LocalMachine.OpenSubKey(path + @"\" + subkey2);
					string[]? subkeys2 = rk2?.GetSubKeyNames();
					rk2?.Close();
					foreach (string subkey3 in subkeys2)
					{
						RegistryKey? rk3 = Registry.LocalMachine.OpenSubKey(path + @"\" + subkey2 + @"\" + subkey3 + @"\Device Parameters");
						string? readComName = (string?)rk3?.GetValue("PortName");
						int? latencyTimer = (int?)rk3?.GetValue("LatencyTimer");
						rk3?.Close();
						if (readComName?.ToLower() == comName.ToLower() && latencyTimer.HasValue)
							return latencyTimer.Value;
					}
				}
			}
			catch { }
			return -1;
#pragma warning restore CA1416 // Walidacja zgodności z platformą
		}

		public static bool SetComTimeoutInRegistry(string comName, int newTimeout)
		{
			try
			{
#pragma warning disable CA1416 // Walidacja zgodności z platformą
				string path = @"SYSTEM\ControlSet001\Enum\FTDIBUS";
				RegistryKey? rk = Registry.LocalMachine.OpenSubKey(path);
				string[]? subkeys = rk?.GetSubKeyNames();
				rk?.Close();
				foreach (string subkey2 in subkeys)
				{
					RegistryKey? rk2 = Registry.LocalMachine.OpenSubKey(path + @"\" + subkey2);
					string[]? subkeys2 = rk2?.GetSubKeyNames();
					rk2?.Close();
					foreach (string subkey3 in subkeys2)
					{
						RegistryKey? rk3 = Registry.LocalMachine.OpenSubKey(
								path + @"\" + subkey2 + @"\" + subkey3 + @"\Device Parameters", true);
						string? readComName = (string?)rk3?.GetValue("PortName");
						if (readComName?.ToLower() == comName.ToLower())
						{
							rk3?.SetValue("LatencyTimer", newTimeout);
							rk3?.Close();
							return true;
						}
						rk3?.Close();
					}
				}
			}
			catch { }
			return false;
#pragma warning restore CA1416 // Walidacja zgodności z platformą
		}
	}
}

/*
	NuGet console:
	Install-Package System.Management
*/
