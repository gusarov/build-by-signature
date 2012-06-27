using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace MyUtils.UAC
{
	public static class StreamHelper
	{
		public static byte[] ReceivePackage(this NetworkStream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			var lenArray = new byte[4];
			var i = stream.Read(lenArray, 0, lenArray.Length);
			//(i == 4).Ensure();
			var total = 0;
			var len = BitConverter.ToInt32(lenArray, 0);
			if (len <= 0)
			{
				throw new Exception("Bad Package");
			}

			if (len > 1 * 1024 * 1024)
			{
				throw new Exception("Package too large");
			}

			var data = new byte[len];
			while (total < len)
			{
				i = stream.Read(data, total, data.Length - total);
				total += i;
			}
			return data;
		}

		public static void SendPackage(this NetworkStream stream, byte[] package)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			if (package == null)
			{
				throw new ArgumentNullException("package");
			}

			var len = package.Length;
			var lenArray = BitConverter.GetBytes(len);
			//(4 == lenArray.Length).Ensure();

			stream.Write(lenArray, 0, lenArray.Length);
			stream.Write(package, 0, package.Length);
		}

		public static string Utf8(this byte[] bytes)
		{
			return Encoding.UTF8.GetString(bytes);
		}

		public static byte[] Utf8(this string str)
		{
			if (str == null)
			{
				return new byte[0];
			}
			return Encoding.UTF8.GetBytes(str);
		}

		public static string[] Parse(this string msg, int parts, char splitter)
		{
			return ParseIterator(msg, parts, splitter).ToArray();
		}

		public static IEnumerable<string> ParseIterator(this string msg, int parts, char splitter)
		{
			int index = 0;
			for (int i = 0; i < parts - 1; i++)
			{
				var indexNext = msg.IndexOf(splitter, index);
				if (index < 0)
				{
					throw new ArgumentException("Have only " + i + " parts, but " + parts + " specified");
				}
				yield return msg.Substring(index, indexNext - index);
				index = indexNext + 1;
			}
			yield return msg.Substring(index);
		}
	}
}
