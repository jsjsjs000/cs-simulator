using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
	class DeviceTreeNode
	{
		public uint address;
		public bool isWireModule;
		public bool isWirelessModule;
		public bool isMaster;
		public bool isLogCollector;
		public Commands.DeviceVersion.HardwareType hardwareType = Commands.DeviceVersion.HardwareType.None;
		public List<List<DeviceTreeNode>> wireModules = new List<List<DeviceTreeNode>>();
		public List<DeviceTreeNode> wirelessModules = new List<DeviceTreeNode>();
		public List<List<uint>> wireAddresses = new List<List<uint>>();
		public List<uint> wirelessAddresses = new List<uint>();
		public List<DeviceTreeNode> cuClientsList = new List<DeviceTreeNode>();

		public DeviceTreeNode(Commands.DeviceVersion.HardwareType hardwareType = Commands.DeviceVersion.HardwareType.None,
				int wireModulesCount = 0, bool isWirelessModules = false)
		{
			this.hardwareType = hardwareType;
			this.isWireModule = wireModulesCount > 0;
			this.isWirelessModule = isWirelessModules;
			for (int i = 0; i < wireModulesCount; i++)
			{
				wireModules.Add(new List<DeviceTreeNode>());
				wireAddresses.Add(new List<uint>());
			}
		}

		public void GenerateAddresses()
		{
			foreach (List<uint> addresses in wireAddresses)
				addresses.Clear();
			wirelessAddresses.Clear();
			for (int i = 0; i < wireModules.Count; i++)
				foreach (DeviceTreeNode node in wireModules[i])
				{
					wireAddresses[i].Add(node.address);
					node.GenerateAddresses();
					foreach (List<uint> addresses in node.wireAddresses)
						wireAddresses[i].AddRange(addresses);
					//if (i < node.wireAddresses.Count)
					//wireAddresses[i].AddRange(node.wireAddresses[i]);
				}
			foreach (DeviceTreeNode node in wirelessModules)
			{
				wirelessAddresses.Add(node.address);
				node.GenerateAddresses();
				wirelessAddresses.AddRange(node.wirelessAddresses);
			}
		}
	}
}
