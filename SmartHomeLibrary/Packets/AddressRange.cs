using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHomeTool.SmartHomeLibrary
{
	class AddressRange
	{
		public const string ExampleText = "Eg. '1,2,3' or '1-9,15,20-30'"; // $$ lang

		public static bool Parse(string s, out List<byte> list)
		{
			list = new List<byte>();
			string[] s2 = s.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string s2_ in s2)
			{
				string[] s3 = s2_.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
				byte a1, a2;
				if (s3.Length == 1 && byte.TryParse(s2_, out a1))
					list.Add(a1);
				else if (s3.Length == 2 && byte.TryParse(s3[0], out a1) && byte.TryParse(s3[1], out a2))
				{
					for (int i = Math.Min(a1, a2); i <= Math.Max(a1, a2); i++)
						if (i <= 255)
							list.Add((byte)i);
				}
				else
					return false;
			}
			list.Sort();
			Common.RemoveDuplicatesFromSortedList(list);
			return true;
		}

		public static bool Parse(string s, out string hint, out int color, out List<byte> list)
		{
			list = new List<byte>();
			if (s.Length == 0)
			{
				hint = ExampleText;
				color = 0x000000; /// black
				return false;
			}

			if (!Parse(s, out list))
			{
				hint = "Input error";
				color = 0xff0000; /// red
				return false;
			}

			hint = "";
			color = 0x000000; /// black
			return true;
		}
	}
}
