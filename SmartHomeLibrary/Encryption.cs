using ICSharpCode.SharpZipLib.BZip2;
using SmartHomeTool.SmartHomeLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleSmartHomeTool.SmartHomeLibrary
{
	internal class Encryption
	{
		readonly static byte[] Key = new byte[]
		{
			199, 78, 100, 73, 210, 37, 141, 67,
			146, 154, 130, 43, 11, 161, 172, 245,
			177, 161, 164, 141, 130, 23, 150, 85,
			73, 150, 13, 75, 234, 83, 167, 175,
		};
		readonly static byte[] IV = new byte[]
		{
			47, 174, 15, 115, 86, 58, 189, 228,
			35, 85, 192, 211, 91, 45, 81, 188
		};

		public enum SerialType { None, ServiceTool };

		public static Log? logError = null;

		public static string Encrypt(string text)
		{
			using Aes myAes = Aes.Create();
			try
			{
					/// https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=net-6.0
				using Aes aesAlg = Aes.Create();
				aesAlg.Key = Key;
				aesAlg.IV = IV;
				ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
				using MemoryStream msEncrypt = new();
				using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
				using (StreamWriter swEncrypt = new(csEncrypt))
				{
					swEncrypt.Write(text);
				}
				byte[] encrypted = msEncrypt.ToArray();
				return Convert.ToBase64String(encrypted); ;
			}
			catch (Exception ex)
			{
				if (logError != null)
					logError.WriteDateTimeLog("", ex.ToString());
				return "";
			}
		}

		public static string Decrypt(string encrypted)
		{
			if (encrypted.Length == 0)
				return "";

			try
			{
				byte[] encryptedBytes = Convert.FromBase64String(encrypted);
				string text = "";
				using (Aes aesAlg = Aes.Create())
				{
					aesAlg.Key = Key;
					aesAlg.IV = IV;
					ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
					using MemoryStream msDecrypt = new(encryptedBytes);
					using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
					using StreamReader srDecrypt = new(csDecrypt);
					text = srDecrypt.ReadToEnd();
				}
				return text;
			}
			catch (Exception ex)
			{
				if (logError != null)
					logError.WriteDateTimeLog("", ex.ToString());
				return "";
			}
		}

		public static string Compress(string str)
		{
			MemoryStream mIn = new(Encoding.UTF8.GetBytes(str))
			{
				Position = 0
			};
			MemoryStream mOut = new();
			BZip2.Compress(mIn, mOut, false, 1);
			mOut.Position = 0;
			byte[] mOutData = new byte[mOut.Length];
			mOut.Read(mOutData, 0, (int)mOut.Length);
			return Convert.ToBase64String(mOutData);
		}

		public static string Decompress(string str)
		{
			MemoryStream mIn = new(Convert.FromBase64String(str));
			MemoryStream mOut = new();
			BZip2.Decompress(mIn, mOut, false);
			byte[] mOutData = new byte[mOut.Length];
			mOut.Position = 0;
			mOut.Read(mOutData, 0, (int)mOut.Length);
			return Encoding.UTF8.GetString(mOutData);
		}

		public static bool CheckHardwareId(string tx)
		{
			return ParseHardwareId(tx, out _, out _);
		}

		static int? ParseHardwareIdPart(string part4)
		{
			if (part4.Length != 4)
				return null;

			int result = 0;
			for (int i = 0; i < part4.Length; i++)
				if ((int)part4[i] >= (int)'0' && (int)part4[i] <= (int)'9' ||
						(int)part4[i] >= (int)'A' && (int)part4[i] <= (int)'F')
				{
					byte a;
					if (!byte.TryParse(part4[i].ToString(), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out a))
						return null;
					result |= a << (4 * (part4.Length - i - 1));
				}
				else
					return null;
			return result;
		}

		public static bool ParseHardwareId(string tx, out uint a1, out uint a2)
		{
			a1 = 0;
			a2 = 0;
			try
			{
				string id = tx.ToUpper();
				if (id.Length != 19 || id[4] != '-' || id[9] != '-' || id[14] != '-')
					return false;

				int? id1 = ParseHardwareIdPart(tx.Substring(0, 4));
				int? id2 = ParseHardwareIdPart(tx.Substring(5, 4));
				int? id3 = ParseHardwareIdPart(tx.Substring(10, 4));
				int? id4 = ParseHardwareIdPart(tx.Substring(15, 4));
				if (!id1.HasValue || !id2.HasValue || !id3.HasValue || !id4.HasValue)
					return false;

				a1 = (uint)((id1.Value << 16) | id2.Value);
				a2 = (uint)((id3.Value << 16) | id4.Value);
			}
			catch (Exception ex)
			{
				if (logError != null)
					logError.WriteDateTimeLog("", ex.ToString());
				return false;
			}
			return true;
		}

		public static string EncryptSerial(uint id1, uint id2, SerialType serialType)
		{
			if (serialType == SerialType.None)
				return "";

			if (serialType == SerialType.ServiceTool)
			{
				id1 ^= 0x73e810ecU;
				id2 ^= 0x84f195abU;
			}
			return Encrypt($"{id1:x8}{id2:x8}");
		}

		public static bool DecryptSerial(string serial, SerialType serialType, out uint id1, out uint id2)
		{
			id1 = 0;
			id2 = 0;
			if (serialType == SerialType.None)
				return false;

			try
			{
				string idO = Decrypt(serial);
				if (idO.Length != 16)
					return false;

				id1 = uint.Parse(idO[..8], NumberStyles.HexNumber);
				id2 = uint.Parse(idO[8..], NumberStyles.HexNumber);
				if (serialType == SerialType.ServiceTool)
				{
					id1 ^= 0x73e810ecU;
					id2 ^= 0x84f195abU;
				}
				return true;
			}
			catch (Exception ex)
			{
				if (logError != null)
					logError.WriteDateTimeLog("", ex.ToString());
				return false;
			}
		}

		public static bool GetHardwareId(out uint id1, out uint id2)
		{
			id1 = 0;
			id2 = 0;
			string uuid = "";
#pragma warning disable CA1416 // Walidacja zgodności z platformą
#pragma warning disable CS8600 // Konwertowanie literału null lub możliwej wartości null na nienullowalny typ.
			try
			{
				ManagementClass mc = new("Win32_ComputerSystemProduct");
				foreach (ManagementObject mo in mc.GetInstances().Cast<ManagementObject>())
					uuid = mo.Properties["UUID"].Value.ToString();
			}
			catch { }

			string processorId = "";
			try
			{
				ManagementClass mc2 = new("Win32_Processor");
				foreach (ManagementObject mo2 in mc2.GetInstances().Cast<ManagementObject>())
					processorId = mo2.Properties["ProcessorId"].Value.ToString();
			}
			catch { }

			string bios = "";
			try
			{
				ManagementClass mc3 = new("Win32_BIOS");
				foreach (ManagementObject mo3 in mc3.GetInstances().Cast<ManagementObject>())
					bios = mo3.Properties["SerialNumber"].Value.ToString();
			}
			catch { }

			string motherBoard = "";
			try
			{
				ManagementClass mc4 = new("Win32_BaseBoard");
				foreach (ManagementObject mo4 in mc4.GetInstances().Cast<ManagementObject>())
					motherBoard = mo4.Properties["SerialNumber"].Value.ToString();
			}
			catch { }
#pragma warning restore CS8600 // Konwertowanie literału null lub możliwej wartości null na nienullowalny typ.
#pragma warning restore CA1416 // Walidacja zgodności z platformą

			id1 = 0x39a39fc9;
			id2 = 0x5236ad82;
			int i = 0;
			foreach (char c in motherBoard + processorId)
			{
				id1 ^= (uint)((byte)c << (i % 28));
				i += 8;
			}
			i = 0;
			foreach (char c in bios + uuid)
			{
				id2 ^= (uint)((byte)c << (i % 28));
				i += 8;
			}
			return true;
		}

		public static string HardwareIdToString(uint id1, uint id2)
		{
			return (id1 >> 16).ToString("x4").ToUpper() + "-" + (id1 & 0xffff).ToString("x4").ToUpper() + "-" +
					(id2 >> 16).ToString("x4").ToUpper() + "-" + (id2 & 0xffff).ToString("x4").ToUpper();
		}

		public static string GetRandomString(int length, bool extended)
		{
			string result = "";
			string characters = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
			if (extended)
				characters += "!@#$%^&*()_+=-[]{}\\|:,.<>/?";
			Random rand = new();
			for (int i = 0; i < length; i++)
				result += characters[rand.Next(characters.Length)];
			return result;
		}

		public static string GetSHA512(string inputString)
		{
			SHA512 sha512 = SHA512.Create();
			byte[] bytes = Encoding.UTF8.GetBytes(inputString);
			byte[] hash = sha512.ComputeHash(bytes);
			return GetStringFromHash(hash);
		}

		private static string GetStringFromHash(byte[] hash)
		{
			StringBuilder result = new StringBuilder();
			for (int i = 0; i < hash.Length; i++)
				result.Append(hash[i].ToString("X2"));
			return result.ToString();
		}
	}
}
