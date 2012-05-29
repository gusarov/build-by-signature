using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using BuildBySignature;
using System.IO;
using Sample;

namespace BuildBySignature_ConsoleTest
{
	class Program
	{
		static void Main()
		{
			var sw = Stopwatch.StartNew();
			var hash = Hasher.Hash(typeof(MyUtils).Assembly.Location);
			sw.Stop();
			Console.WriteLine("0x{0:X8}", hash);
			Console.WriteLine(sw.ElapsedMilliseconds + " ms");
		}
	}
}
