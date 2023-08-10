using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
	public class ThreadClass
	{
		protected Thread thread;
		public bool ExitThread = false;
		public bool ExitedThread = false;

		public ThreadClass()
		{
			thread = new Thread(new ThreadStart(ThreadProc));
		}

		public void StartThread()
		{
			thread.Start();
		}

		protected virtual void ThreadProc()
		{
			Common.SetDateFormat();
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
	}
}
