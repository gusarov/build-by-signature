using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Threading;

using BuildBySignature;

using Ooze.Common;

namespace Test
{
//	class X : Outer
//	{
//		public X()
//		{
//			var q = typeof(NestedProtectedInternal);
//		}
//		class X2 : NestedProtectedInternal
//		{
//			public X2()
//			{
//				//base.x();
//			}
//		}
//	}

//	class q : Sealed
//	{
//		
//	}

	class Program
	{


		static void Main()
		{
			var q = typeof(Sealed);
			// var asm = typeof(ReallyBigProject.Class1).Assembly;
			// var asm = typeof(int).Assembly;
			var asm = typeof(Window).Assembly;
			// var asm = typeof(BaseDispatcher).Assembly;

			var sw = Stopwatch.StartNew();

			int hash = 0;
			Hasher.Hash(asm, ref hash);

			sw.Stop();

			Console.WriteLine();
			Console.WriteLine(sw.ElapsedMilliseconds + "\tms");
			Console.WriteLine(Hasher._ctxTypes + "\tTypes");
			Console.WriteLine(Hasher._ctxMembers + "\tMembers");
			Console.WriteLine();

		}
	}
}
