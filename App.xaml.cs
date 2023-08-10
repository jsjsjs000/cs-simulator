using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace InteligentnyDomSimulator
{
	public partial class App : Application
	{
		public const string ProgramName = "Smart Home Simulator";
		public const int ProgramVersionMajor = 1;
		public const int ProgramVersionMinor = 0;
		public static readonly int ProgramVersionBuild = Assembly.GetExecutingAssembly().GetName().Version!.Build;
		public static readonly int ProgramVersionRevision = Assembly.GetExecutingAssembly().GetName().Version!.Revision;
		public static readonly DateTime ProgramBuildDateTime = new DateTime(2000, 1, 1)
				.AddDays(Assembly.GetExecutingAssembly().GetName().Version!.Build)
				.AddSeconds(Assembly.GetExecutingAssembly().GetName().Version!.Revision * 2);
	}
}
