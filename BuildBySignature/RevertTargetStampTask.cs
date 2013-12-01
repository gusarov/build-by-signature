using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BuildBySignature
{
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