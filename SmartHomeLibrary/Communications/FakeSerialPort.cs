using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartHomeTool.SmartHomeLibrary
{
	public enum Parity { None }
	public enum StopBits { One }

	public class SerialPort
	{
		public int ReadTimeout;
		public int WriteTimeout;

#pragma warning disable IDE0060 // Usuń nieużywany parametr
		public SerialPort(string comName, int baudRate, Parity parity, int bits, StopBits stopBits) { }
		public void Open() { }
		public void Close() { }
		public bool IsOpen;
		public int Read(byte[] buffer, int offset, int count) { return 0; }
		public int Write(byte[] send, int offset, int count) { return 0; }
		public int BytesToRead;
#pragma warning restore IDE0060 // Usuń nieużywany parametr
	}
}
