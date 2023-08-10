using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
	public class Communication : ThreadClass
	{
		protected const int BufferLength = 16384;
		public int DefaultReadTimeoutMs = 30;
		public int ReadTimeoutMs;
		public const int ReadTimeoutEpromMs = 250; // 60 ms for STM32 ComPort in Windows, 250 ms for ESP32 Ethernet
		public const int ReadTimeoutEndProgramming = 450; // 200 ms for STM32, 450 ms for ESP32 Ethernet
		public const int ReadTimeoutGetDeviceAddressWithDelay = 1050;
		public int BeforePacketDelay = 0;
		const int BaudRate = 115200;

		public enum ConnectionType { Com, Tcp };

		public ConnectionType connectionType = ConnectionType.Com;
		public SerialPort com;
		public Socket tcp;

		protected byte[] receiveBuffer = new byte[BufferLength];
		protected int receiveBufferIndex = 0;
		protected bool waitingForAnswer = false;
		protected object waitingForAnswerLock = new();

		public string ComName = "COM1";
		public string Ip = "";
		public ushort Port = 28844;

		public bool CanLogPackets = false;
		public bool CanLogWrongPackets = false;
		public bool SendBytesOneByOneWithDelay1ms = false;
		public Queue<PacketLog> packetsLogQueue = new();

		public int lastReceiveMiliseconds;

		public Commands cmd;

		public Communication() : base()
		{
			ReadTimeoutMs = DefaultReadTimeoutMs;
			cmd = new Commands(this);
			this.StartThread();
		}

		public void SetReadTimeOut(int timeout)
		{
			this.ReadTimeoutMs = timeout;
		}

		public void SetDefaultReadTimeOut()
		{
			this.ReadTimeoutMs = DefaultReadTimeoutMs;
		}

		public bool Connect()
		{
			Disconnect();

			try
			{
				if (connectionType == ConnectionType.Com)
				{
					com = new SerialPort(ComName, BaudRate, Parity.None, 8, StopBits.One)
					{
						ReadTimeout = ReadTimeoutMs,
						WriteTimeout = ReadTimeoutMs
					};
					com.Open();
				}
				if (connectionType == ConnectionType.Tcp)
				{
					//IPHostEntry ipHostInfo = Dns.GetHostEntry(Ip);
					//IPAddress ipAddress = ipHostInfo.AddressList[0];
					//tcp = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					tcp.Connect(Ip, Port);
				}
			}
			catch // (Exception ex)
			{
				// Console.WriteLine(ex.ToString());
				return false;
			}
			return true;
		}

		public void Disconnect()
		{
			try
			{
				com?.Close();
				com = null;

				if (tcp != null)
				{
					tcp.Shutdown(SocketShutdown.Both);
					tcp.Close();
				}
				tcp = null;
			}
			catch { }
		}

		public bool IsConnected()
		{
			if (connectionType == ConnectionType.Com)
				return com != null && com.IsOpen;
			else
				return tcp != null && tcp.Connected;
		}

		public bool SendPacket(uint packetId, uint encryptionKey, uint address, bool isAnswer, byte[] data)
		{
			byte[] send = Packets.EncodePacket(packetId, encryptionKey, address, data, isAnswer);
			return Send(send);
		}

		public bool Send(byte[] send)
		{
			try
			{
				if (SendBytesOneByOneWithDelay1ms)
					for (int i = 0; i < send.Length; i++)
					{
						if (connectionType == ConnectionType.Com)
							com.Write(new byte[] { send[i] }, 0, 1);
						else
							tcp.Send(new byte[] { send[i] });
						Thread.Sleep(1);
					}
				else
				{
					if (connectionType == ConnectionType.Com)
						com.Write(send, 0, send.Length);
					else
						tcp.Send(send);
				}

				if (CanLogPackets)
				{
					lock (packetsLogQueue)
						packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Packet, DateTime.Now, send, Packets.PacketDirection.Out));
					string comment = PacketsComments.DecodeFrameAndGetComment(send, out bool isError);
					if (comment.Length > 0)
						lock (packetsLogQueue)
							packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Debug, DateTime.Now, Array.Empty<byte>(),
									Packets.PacketDirection.None, " " + comment, isError));
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		public bool SendPacket(uint packetId, uint encryptionKey, uint address, byte[] data,
				out uint outPacketId, out uint outEncryptionKey, out uint outAddress, out byte[] outData)
		{
			byte[] send = Packets.EncodePacket(packetId, encryptionKey, address, data, false);
			return Send(send, out outPacketId, out outEncryptionKey, out outAddress, out outData);
		}

		public bool SendPacket(uint packetId, uint encryptionKey, uint address, byte[] data,
				out uint outPacketId, out uint outEncryptionKey, out uint outAddress, out List<byte[]> outDatas,
				int timeout = 0)
		{
			byte[] send = Packets.EncodePacket(packetId, encryptionKey, address, data, false);
			return Send(send, out outPacketId, out outEncryptionKey, out outAddress, out outDatas, timeout);
		}

		void Send_ReadTrashData()
		{
			while (connectionType == ConnectionType.Com && com.BytesToRead > 0)
			{
				byte[] trash = new byte[1024];
				_ = com.Read(trash, 0, trash.Length);
				Thread.Sleep(3);
			}
			while (connectionType == ConnectionType.Tcp && tcp.Available > 0)
			{
				byte[] trash = new byte[1024];
				_ = tcp.Receive(trash, 0, trash.Length, SocketFlags.None);
				Thread.Sleep(3);
			}
		}

		bool Send_SendData(byte[] send)
		{
			try
			{
				if (connectionType == ConnectionType.Com)
					com.Write(send, 0, send.Length);
				else
					_ = tcp.Send(send);
				if (CanLogPackets)
				{
					lock (packetsLogQueue)
						packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Packet, DateTime.Now, send, Packets.PacketDirection.Out));
					string comment = PacketsComments.DecodeFrameAndGetComment(send, out bool isError);
					if (comment.Length > 0)
						lock (packetsLogQueue)
							packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Debug, DateTime.Now, Array.Empty<byte>(), Packets.PacketDirection.None,
									" " + comment, isError));
				}
				return true;
			}
			catch
			{
				waitingForAnswer = false;
				return false;
			}
		}

		bool Send_Receive(byte[] send, bool onlyOneAnswer, out uint outPacketId, out uint outEncryptionKey,
				out uint outAddress, out List<byte[]> outDatas)
		{
			outPacketId = 0;
			outEncryptionKey = 0;
			outAddress = 0;
			outDatas = new List<byte[]>();

			try
			{
				DateTime startRead = DateTime.Now;
				receiveBufferIndex = 0;
				int prevReceiveBufferIndex = receiveBufferIndex;
				while (DateTime.Now.Subtract(startRead).TotalMilliseconds < ReadTimeoutMs)
				{
					if (connectionType == ConnectionType.Com && com.BytesToRead > 0 ||
							connectionType == ConnectionType.Tcp && tcp.Available > 0)
					{
						if (connectionType == ConnectionType.Com &&
								receiveBufferIndex + com.BytesToRead >= receiveBuffer.Length ||
								connectionType == ConnectionType.Tcp &&
								receiveBufferIndex + tcp.Available >= receiveBuffer.Length)  /// buffer overflow
						{
							receiveBufferIndex = 0;
							prevReceiveBufferIndex = 0;
						}

						if (connectionType == ConnectionType.Com)
							receiveBufferIndex += com.Read(receiveBuffer, receiveBufferIndex, com.BytesToRead);
						else
							receiveBufferIndex += tcp.Receive(receiveBuffer, receiveBufferIndex, tcp.Available, SocketFlags.None);

						if (Packets.FindFrameAndDecodePacketInBuffer(receiveBuffer, receiveBufferIndex, out outPacketId,
								out outEncryptionKey, out outAddress, out byte[] outData, out bool isAnswer) && outData.Length >= 1 && isAnswer)
						{
							byte[] received = new byte[receiveBufferIndex - prevReceiveBufferIndex];
							Array.Copy(receiveBuffer, prevReceiveBufferIndex, received, 0, received.Length);
							if (CanLogPackets)
							{
								lock (packetsLogQueue)
									packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Packet, DateTime.Now, received, Packets.PacketDirection.In));

								string comment = PacketsComments.DecodeFrameAndGetComment(received, out bool isError);
								lock (packetsLogQueue)
									if (comment.Length > 0)
										packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Debug, DateTime.Now, Array.Empty<byte>(), Packets.PacketDirection.None,
												" " + comment + Environment.NewLine, isError));
									else
										packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Debug, DateTime.Now, Array.Empty<byte>(), Packets.PacketDirection.None,
												"", isError));
							}

							outDatas.Add(outData);
							receiveBufferIndex = 0;
							waitingForAnswer = false;
							lastReceiveMiliseconds = (int)DateTime.Now.Subtract(startRead).TotalMilliseconds;
							if (onlyOneAnswer)
								return true;
						}
					}
					Thread.Sleep(1);
				}

				if (outDatas.Count == 0)
				{
					if (!CanLogPackets && CanLogWrongPackets)
						lock (packetsLogQueue)
						{
							packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Packet, DateTime.Now, send, Packets.PacketDirection.Out));
							string comment = PacketsComments.DecodeFrameAndGetComment(send, out bool isError);
							if (comment.Length > 0)
								packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Debug, DateTime.Now, Array.Empty<byte>(), Packets.PacketDirection.None,
										" " + comment, isError));
						}

					byte[] received2 = new byte[receiveBufferIndex - prevReceiveBufferIndex];
					Array.Copy(receiveBuffer, prevReceiveBufferIndex, received2, 0, received2.Length);
					if (CanLogPackets || CanLogWrongPackets)
						lock (packetsLogQueue)
							packetsLogQueue.Enqueue(new PacketLog(PacketLog.Type.Packet, DateTime.Now, received2, Packets.PacketDirection.In));
				}
			}
			catch { }

			receiveBufferIndex = 0;
			waitingForAnswer = false;
			return outDatas.Count > 0;
		}

		public bool Send(byte[] send, out uint outPacketId, out uint outEncryptionKey,
				out uint outAddress, out byte[] outData)
		{
			outPacketId = 0;
			outEncryptionKey = 0;
			outAddress = 0;
			outData = Array.Empty<byte>();
			if (!IsConnected())
				return false;

			lock (waitingForAnswerLock)
			{
				if (BeforePacketDelay > 0)
					Thread.Sleep(BeforePacketDelay);

				waitingForAnswer = true;
				lastReceiveMiliseconds = 0;

				Send_ReadTrashData();

				if (!Send_SendData(send))
					return false;

				if (Send_Receive(send, true, out outPacketId, out outEncryptionKey, out outAddress, out List<byte[]> outDatas))
				{
					outData = outDatas[0];
					return true;
				}
				return false;
			}
		}

		public bool Send(byte[] send, out uint outPacketId, out uint outEncryptionKey,
				out uint outAddress, out List<byte[]> outDatas, int timeout = 0)
		{
			outPacketId = 0;
			outEncryptionKey = 0;
			outAddress = 0;
			outDatas = new List<byte[]>();
			if (!IsConnected())
				return false;

			lock (waitingForAnswerLock)
			{
				if (BeforePacketDelay > 0)
					Thread.Sleep(BeforePacketDelay);

				waitingForAnswer = true;
				lastReceiveMiliseconds = 0;

				Send_ReadTrashData();

				if (!Send_SendData(send))
					return false;

				if (timeout > 0)
					SetReadTimeOut(timeout);
				bool ok = Send_Receive(send, false, out outPacketId, out outEncryptionKey, out outAddress, out outDatas);
				SetDefaultReadTimeOut();
				return ok;
			}
		}
	}
}
