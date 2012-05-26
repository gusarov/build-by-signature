using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BuildBySignature
{
	public class GenerateHashTask : AppDomainIsolatedTask
	{
		[Required]
		public string TargetPath { get; set; }

		[Required]
		public string OutputPath { get; set; }

		public override bool Execute()
		{
			var asm = Assembly.Load(File.ReadAllBytes(TargetPath));
			var hash = Hasher.Hash(asm).ToString("X8");
			var now = "";
			if (File.Exists(OutputPath))
			{
				now = File.ReadAllText(OutputPath);
			}
			if (now != hash)
			{
				if (File.Exists(OutputPath))
				{
					File.SetAttributes(OutputPath, FileAttributes.Normal);
				}
				File.WriteAllText(OutputPath, hash);
				File.SetAttributes(OutputPath, FileAttributes.Normal | FileAttributes.Hidden);
			}
			return true;
		}

	}
}