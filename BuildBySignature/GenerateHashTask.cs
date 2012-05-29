using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Reflection;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BuildBySignature
{
	public class GenerateHashTask : Task
	{
		[Required]
		public string TargetPath { get; set; }

		[Required]
		public string OutputPath { get; set; }

		public override bool Execute()
		{
			var hash = Hasher.Hash(TargetPath, Log).ToString("X8");
			if (File.Exists(OutputPath))
			{
				File.SetAttributes(OutputPath, FileAttributes.Normal);
			}
			File.WriteAllText(OutputPath, hash); // we can not omit owerwriting here even on the same data because we should be abel to distinguish a fact that hash is up to date (not disabled)
			File.SetAttributes(OutputPath, FileAttributes.Normal | FileAttributes.Hidden);
			return true;
		}

	}
}