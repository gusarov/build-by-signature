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
		public string BbsPath { get; set; }

		public bool RevertTargetStamp{ get; set; }

		public override bool Execute()
		{
			var hash = Hasher.Hash(TargetPath, Log).ToString("X8");
			string currentHash = null;
			string currentStamp = null;
			if (File.Exists(BbsPath))
			{
				// compatibility - back to normal files due to performance reasons
				if (File.GetAttributes(BbsPath) != FileAttributes.Normal)
				{
					File.SetAttributes(BbsPath, FileAttributes.Normal);
				}
				var current = File.ReadAllLines(BbsPath);
				currentHash = current[0];
				currentStamp = current[1];
			}
			var originalDllLastWriteTime = File.GetLastWriteTimeUtc(TargetPath);
			if (string.Equals(hash, currentHash, StringComparison.Ordinal) && RevertTargetStamp && !string.IsNullOrEmpty(currentStamp))
			{
				File.SetLastWriteTimeUtc(TargetPath, DateTime.ParseExact(currentStamp, "o", CultureInfo.InvariantCulture).ToUniversalTime());
				Log.LogMessage(MessageImportance.High, " * Bbs GenerateHashTask: Target LastWriteTime Reverted! {0} (#{1} {2})", Path.GetFileName(TargetPath), currentHash, currentStamp);
			}
			else
			{
				File.WriteAllText(BbsPath, hash + Environment.NewLine + File.GetLastWriteTimeUtc(TargetPath).ToString("o", CultureInfo.InvariantCulture));
			}

			// bbs timestamps should be related to dll timestamps so that it would be possible to detect dll content change by bbs modification date
			File.SetLastWriteTimeUtc(BbsPath, originalDllLastWriteTime);
			return true;
		}

	}

	public class RevertTargetStampTask : Task
	{
		[Required]
		public string TargetPath { get; set; }

		[Required]
		public string BbsPath { get; set; }

		public override bool Execute()
		{
			if (File.Exists(BbsPath))
			{
				File.SetAttributes(BbsPath, FileAttributes.Normal);
				var current = File.ReadAllLines(BbsPath);
				string currentHash = current[0];
				string currentStamp = current[1];
				var originalDllLastWriteTime = File.GetLastWriteTimeUtc(TargetPath);
				var currentStampDt = DateTime.ParseExact(currentStamp, "o", CultureInfo.InvariantCulture).ToUniversalTime();
				if (originalDllLastWriteTime > currentStampDt)
				{
					File.SetLastWriteTimeUtc(TargetPath, currentStampDt);
					Log.LogMessage(MessageImportance.High, " * Bbs RevertTargetStampTask: Target LastWriteTime Reverted! {0} (#{1} {2})", Path.GetFileName(TargetPath), currentHash, currentStamp);
				}
			}
			return true;
		}

	}
}