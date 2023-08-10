using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using SmartHomeTool.SmartHomeLibrary;

namespace SmartHomeTool.SmartHomeWinLibrary
{
	public partial class PacketsLogControl : UserControl
	{
		const int MaxLogLines = 1000;
		const int DeleteLines = 50;

		public string LangLogQueueSize = "Log queue size";

		public List<Queue<PacketLog>> logQueues = new List<Queue<PacketLog>>();
		public Log logPackets;
		public WinPacketToString winPacketToString = new WinPacketToString();

		public bool ExitThread = false;
		public bool ExitedThread = true;
		Thread thread;

		public int totalTransmitted = 0;
		public int totalReceived = 0;

		public bool isAutoScrollChecked => tbAutoScroll.IsChecked.Value;
		public bool isDecodeCommentsChecked => tbDecodeComments.IsChecked.Value;
		public bool isSaveLogToFileChecked => tbSaveLogToFile.IsChecked.Value;
		public RichTextBox getRichText => richText;
		//public int toolStripHeight => toolStrip.Height;

		public PacketsLogControl()
		{
			InitializeComponent();

			thread = new Thread(new ThreadStart(ThreadProc));
			thread.Start();
		}

		private void ToolBar_Loaded(object sender, RoutedEventArgs e)
		{
			ToolBar toolBar = sender as ToolBar;
			var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
			if (overflowGrid != null)
				overflowGrid.Visibility = Visibility.Collapsed;
			var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
			if (mainPanelBorder != null)
				mainPanelBorder.Margin = new Thickness();
		}

		void ThreadProc()
		{
			ExitedThread = false;
			Common.SetDateFormat();
			DateTime lastLabelQueueSize = DateTime.Now;

			while (!ExitThread)
			{
				int logQueueSize = 0;
				bool updateLabelQueueSize = DateTime.Now.Subtract(lastLabelQueueSize).TotalMilliseconds >= 300;
				if (updateLabelQueueSize)
					lock (logQueues)
						foreach (Queue<PacketLog> logQueue in logQueues)
							logQueueSize += logQueue.Count;

				this.Dispatcher.Invoke(delegate
				{
					if (updateLabelQueueSize)
					{
						tbLogQueue.Text = LangLogQueueSize + ": " + logQueueSize;
						lastLabelQueueSize = DateTime.Now;
						tbTxRx.Text = String.Format("TX: {0}  RX: {1}", totalTransmitted, totalReceived);
					}

					ExecuteQueue();
				});

				Thread.Sleep(3);
			}

			ExitedThread = true;
		}

		public void KillThread()
		{
			try
			{
				//if (thread != null)
				//	thread.Abort(); /// depraced in .NET 5.0
			}
			catch { }
		}

		void ExecuteQueue()
		{
			foreach (Queue<PacketLog> logQueue in logQueues)
			{
				if (logQueue.Count > MaxLogLines)
				{
					lock (logQueue)
						while (logQueue.Count > MaxLogLines - DeleteLines)
							logQueue.Dequeue();
					lock (logQueue)
						logQueue.Enqueue(new PacketLog(PacketLog.Type.Debug, DateTime.Now, new byte[0], Packets.PacketDirection.None,
								"                 <<< too many logs in queue - truncated >>>", false));
				}

				if (logQueue.Count > 0)
				{
					PacketLog packetLog = null;
					lock (logQueue)
						packetLog = logQueue.Dequeue();
					if (ExitThread)
						return;

					if (packetLog != null)
					{
						if (packetLog.type == PacketLog.Type.Packet)
						{
							string s;
							if (tbDebugFrames.IsChecked.Value)
							{
								s = winPacketToString.AddPacketText(richText, packetLog.dt, packetLog.data, packetLog.packetDirection,
										tbAutoScroll.IsChecked.Value);
								RemoveOverflowText();
							}
							else
								s = winPacketToString.GetPacketText(packetLog.dt, packetLog.data, packetLog.packetDirection);
							if (isSaveLogToFileChecked)
								logPackets.WriteLog(s);
							if (packetLog.packetDirection == Packets.PacketDirection.In)
								totalReceived += packetLog.data.Length;
							else if (packetLog.packetDirection == Packets.PacketDirection.Out)
								totalTransmitted += packetLog.data.Length;
						}
						else if (packetLog.type == PacketLog.Type.Debug)
						{
							if (tbDebugFrames.IsChecked.Value && this.isDecodeCommentsChecked)
							{
								winPacketToString.AddDebugText(richText, packetLog.text, packetLog.isError, isAutoScrollChecked);
								RemoveOverflowText();
							}
							if (isSaveLogToFileChecked)
								logPackets.WriteLog(packetLog.text);
						}
					}
				}
			}
		}

		public void RemoveOverflowText()
		{
			if (richText.Document.Blocks.Count > MaxLogLines)
				while (richText.Document.Blocks.Count > MaxLogLines - DeleteLines)
					richText.Document.Blocks.Remove(richText.Document.Blocks.FirstBlock);
		}

		public void ScrollToEnd(bool forceScroll = false)
		{
			if (tbAutoScroll.IsChecked.Value || forceScroll)
				richText.ScrollToEnd();
		}

		void a()
		{
			//IDisposable d = richText.Dispatcher.DisableProcessing();
			DateTime dt = DateTime.Now;
			//IDisposable d = Dispatcher.DisableProcessing();
			//richText.ScrollToEnd();
			TextPointer tp = richText.CaretPosition;
			for (int i = 0; i < 1000; i++)
			{
				TextRange rangeOfText1 = new TextRange(richText.Document.ContentEnd, richText.Document.ContentEnd);
				rangeOfText1.Text = $"Text {i} ";
				rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Blue);
				TextRange rangeOfWord = new TextRange(richText.Document.ContentEnd, richText.Document.ContentEnd);
				rangeOfWord.Text = "word\r\n";
				rangeOfWord.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.GreenYellow);

				//richText.AppendText("aaaaaa\r\n");
				//richText.ScrollToEnd();
				//richText.SelectAll();

				//TextRange text = new TextRange(tp, richText.CaretPosition);
				//TextRange text = richText.Selection;
				//TextPointer current = text.Start.GetInsertionPosition(LogicalDirection.Forward);
				//TextPointer selectionStart = current.GetPositionAtOffset(index, LogicalDirection.Forward);
				//TextPointer selectionEnd = selectionStart.GetPositionAtOffset(keyword.Length, LogicalDirection.Forward);
				//TextRange selection = new TextRange(selectionStart, selectionEnd);

				//if (!string.IsNullOrEmpty(newString))
				//	selection.Text = "aaaaaaaa ";

				//if (i % 2 == 0)
				//	text.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
				//else
				//	text.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Blue);

				//tp = richText.CaretPosition;

				//richText.AppendText("Text1 ");
				//richText.AppendText("word ");
				//richText.AppendText("Text2\r\n");
				//richText.AppendText("Text1 word Text2\r\n");
			}
			//d.Dispose();
			richText.AppendText(" " + (DateTime.Now - dt).TotalMilliseconds.ToString());
			ScrollToEnd();
			//d.Dispose();

			dt = DateTime.Now;
			RemoveOverflowText();
			richText.AppendText(" " + (DateTime.Now - dt).TotalMilliseconds.ToString());
		}

		private void DebugFramesButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void AutoScrollButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void DecodeCommentsButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void SaveLogToFileButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void OpenLogFolderButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Process process = new Process();
				process.StartInfo = new ProcessStartInfo("explorer.exe", "LogPackets");
				process.StartInfo.UseShellExecute = true;
				process.Start();
			}
			catch { }
		}

		private void ClearButton_Click(object sender, RoutedEventArgs e)
		{
			lock (logQueues)
				foreach (Queue<PacketLog> logQueue in logQueues)
					logQueue.Clear();
			richText.Document.Blocks.Clear();
		}

		private void CopySelectedButton_Click(object sender, RoutedEventArgs e)
		{
//a();
			bool selectAll = false;
			if (richText.Selection.IsEmpty)
			{
				richText.SelectAll();
				selectAll = true;
			}
			richText.Copy();
			if (selectAll)
				richText.Selection.Select(richText.Document.ContentEnd, richText.Document.ContentEnd);
		}

		private void ClearTotalButton_Click(object sender, RoutedEventArgs e)
		{
			totalReceived = 0;
			totalTransmitted = 0;
			tbTxRx.Text = "TX: 0  RX: 0";
		}
	}
}
