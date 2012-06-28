using System.Globalization;
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

		public bool RevertTargetStamp{ get; set; }

		public override bool Execute()
		{
			var hash = Hasher.Hash(TargetPath, Log).ToString("X8");
			string currentHash = null;
			string currentStamp = null;
			if (File.Exists(OutputPath))
			{
				File.SetAttributes(OutputPath, FileAttributes.Normal);
				var current = File.ReadAllLines(OutputPath);
				currentHash = current[0];
				currentStamp = current.Length > 1 ? current[1] : null;
			}
			if (string.Equals(hash, currentHash) && RevertTargetStamp && !string.IsNullOrEmpty(currentStamp))
			{
				File.SetLastWriteTimeUtc(TargetPath, DateTime.ParseExact(currentStamp, "o", CultureInfo.InvariantCulture));
				Log.LogMessage(MessageImportance.High, " * Bbs GenerateHashTask: Target LastWriteTime Reverted! {0}", Path.GetFileName(TargetPath));
			}
			// we can not omit owerwriting here even on the same data because we should be abel to distinguish a fact that hash is up to date (not disabled) for incremental build
			File.WriteAllText(OutputPath, hash + Environment.NewLine + File.GetLastWriteTimeUtc(TargetPath).ToString("o", CultureInfo.InvariantCulture));
			File.SetAttributes(OutputPath, FileAttributes.Normal | FileAttributes.Hidden);
			return true;
		}

	}
}