using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
	public class Log
	{
		public enum FilenameFormat { yyyyMMdd, yyyyMMdd_HH }

		const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
		const string DateTimeFormatMs = "yyyy-MM-dd HH:mm:ss.fff";

		const string Separator = "_|_";

		public static readonly ConsoleColor DefaultForgroundColor;
		public static readonly ConsoleColor DefaultBackgroundColor;

		string path;
		string ext;
		string filenameFormat = "yyyy-MM-dd";
		string currentFilename = "";
		FileStream fs;

		public bool AutoFlush = true;
		public bool CanConsoleEcho = true;

		static Log()
		{
			Console.ResetColor();
			DefaultForgroundColor = Console.ForegroundColor;
			DefaultBackgroundColor = Console.BackgroundColor;
		}

		public Log(string path, FilenameFormat filenameFormat, string ext, bool createFileAtStartup = true,
				bool canConsoleEcho = true)
		{
			this.CanConsoleEcho = canConsoleEcho;
			this.path = AppDomain.CurrentDomain.BaseDirectory + path + Path.DirectorySeparatorChar;
			switch (filenameFormat)
			{
				case FilenameFormat.yyyyMMdd: this.filenameFormat = "yyyy-MM-dd"; break;
				case FilenameFormat.yyyyMMdd_HH: this.filenameFormat = "yyyy-MM-dd HH"; break;
			}
			this.ext = ext;

			if (createFileAtStartup)
				WriteStringToFileAndCheckFile("**************** Start " + DateTime.Now.ToString(DateTimeFormat) +
						" ****************" + Environment.NewLine);
		}

		~Log()
		{
			try
			{
				if (fs != null)
					fs.Close();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		string GetLogFileName()
		{
			return DateTime.Now.ToString(filenameFormat);
		}

		object LockWriteStringToFileAndCheckFile = new object();
		void WriteStringToFileAndCheckFile(string s)
		{
			lock (LockWriteStringToFileAndCheckFile)
			{
				if (currentFilename != GetLogFileName())
				{
					try
					{
						if (fs != null)
							fs.Close();
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.ToString());
					}

					try
					{
						if (!Directory.Exists(path))
							Directory.CreateDirectory(path);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.ToString());
					}

					for (int i = 0; i < 1000; i++)
						try
						{
							currentFilename = GetLogFileName();
							if (i > 0)
								currentFilename += "_" + i.ToString();
							fs = File.Open(path + currentFilename + ext, FileMode.OpenOrCreate, FileAccess.ReadWrite,
									FileShare.Read);
							break;
						}
						catch { }

					try
					{
						fs.Position = fs.Length;
						if (fs.Length > 0)
							WriteStringToFile(Environment.NewLine);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.ToString());
					}
				}

				WriteStringToFile(s);
			}
		}

		void WriteStringToFile(string s)
		{
			try
			{
				byte[] b = new byte[s.Length * 2];
				char[] ch = s.ToCharArray();
				Encoder enc = Encoding.UTF8.GetEncoder();
				int i = enc.GetBytes(ch, 0, ch.Length, b, 0, true);
				fs.Write(b, 0, i);
				if (AutoFlush)
					fs.Flush();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}

		public void WriteLog(string text, bool echo = true)
		{
			if (echo && CanConsoleEcho)
				Console.Write(text);
			WriteStringToFileAndCheckFile(text);
		}

		public void WriteLog(string type, string text, bool echo = true)
		{
			if (type.Length == 0)
			{
				WriteLog(text);
				return;
			}
			if (echo && CanConsoleEcho)
				Console.WriteLine(type + " | " + text);
			WriteStringToFileAndCheckFile(type + Separator + text);
		}

		public void WriteDateTimeLogLine(string text, bool echo = true, ConsoleColor? consoleColor = null)
		{
			WriteLogLine(DateTime.Now.ToString() + "." + DateTime.Now.Millisecond.ToString("d3") + ": " +
					text, echo, consoleColor);
		}

		public void WriteLogLine(string text, bool echo = true, ConsoleColor? consoleColor = null)
		{
			if (echo && CanConsoleEcho)
			{
				ConsoleColor? prevColor = null;
				if (consoleColor != null)
				{
					prevColor = Console.ForegroundColor;
					Console.ForegroundColor = consoleColor.Value;
				}
				Console.WriteLine(text);
				if (consoleColor != null)
					Console.ForegroundColor = prevColor.Value;
			}
			WriteStringToFileAndCheckFile(text + Environment.NewLine);
		}

		public void WriteLogLine(string type, string text)
		{
			this.WriteLog(type, text + Environment.NewLine);
		}

		public void WriteDateTimeLog(string type, string text, bool echo = true)
		{
			if (echo && CanConsoleEcho)
			{
				if (type.Length > 0)
					Console.Write(DateTime.Now.ToString() + ": " + type + " | " + text);
				else
					Console.Write(DateTime.Now.ToString() + ": " + text);
			}
			if (type.Length > 0)
				WriteStringToFileAndCheckFile(DateTime.Now.ToString() + Separator + type + Separator + text);
			else
				WriteStringToFileAndCheckFile(DateTime.Now.ToString() + Separator + text);
		}

		static DateTime LogLongDt = DateTime.Now;

		public static void ResetStartLogLong()
		{
			LogLongDt = DateTime.Now;
		}

		public static string GetLogLong()
		{
			return (int)Math.Round(DateTime.Now.Subtract(LogLongDt).TotalMilliseconds) + "ms";
		}

		public static string GetLogLongAndReset()
		{
			string s = GetLogLong();
			ResetStartLogLong();
			return s;
		}

		public static void WriteScreenLogProgress(string s)
		{
			int cursorLeft = Console.CursorLeft;
			int cursorTop = Console.CursorTop;
			Console.Write(s);
			string sPad = "".PadRight(Console.WindowWidth - Console.CursorLeft);
			Console.Write(sPad);
			Console.CursorLeft = cursorLeft;
			Console.CursorTop = cursorTop;
		}
	}
}
