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
	public class BbsCopyLocalReference : Task
	{
		[Required]
		public ITaskItem[] ReferenceCopyLocalPaths { get; set; }

		[Required]
		public string OutDir { get; set; }

		public string BbsDiagLevel
		{
			get { return _bbsDiagLevel; }
			set
			{
				_bbsDiagLevel = value;
				_bbsDiagLevelEnum = (MessageImportance)Enum.Parse(typeof(MessageImportance), value);
			}
		}

		private MessageImportance _bbsDiagLevelEnum;
		private string _bbsDiagLevel;

		public override bool Execute()
		{
			Log.LogMessage(BbsDiagLevel, " * BbsCopyLocalReference # Enter");

			foreach (var reference in ReferenceCopyLocalPaths)
			{
				// we should copy only file that has corresponding .bbs file near them that is actually out of date
				var name = reference.GetMetadata("FileName");
				var dll = reference.GetMetadata("FullPath");
				var dest = Path.Combine(OutDir, reference.GetMetadata("DestinationSubDirectory"));
				var dllTarget = Path.Combine(dest, Path.GetFileName(dll));
				var bbs = reference.GetMetadata("FullPath") + ".bbs";
				var bbsTarget = Path.Combine(dest, Path.GetFileName(bbs));
				var ext = Path.GetExtension(dll);

				if (File.Exists(bbs))
				{
					DateTime bbsd, bbsTargetd;
					if (!File.Exists(bbsTarget))
					{
						Log.LogMessage(BbsDiagLevel, " * BbsCopyLocalReference # [First Time] " + name + ext);
						File.Copy(bbs, bbsTarget, true);
					}
					else if ((bbsd = File.GetLastWriteTimeUtc(bbs)) > (bbsTargetd = File.GetLastWriteTimeUtc(bbsTarget)))
					{
						Log.LogMessage(BbsDiagLevel, " * BbsCopyLocalReference # [Update] " + name + ext + " src_bbs=" + bbsd + " trg_bbs=" + bbsTargetd);
						File.Copy(dll, dllTarget, true);
						File.Copy(bbs, bbsTarget, true);
					}
					else
					{
						Log.LogMessage(BbsDiagLevel, " * BbsCopyLocalReference # [Skip] " + name + ext);
					}
				}
				else
				{
					Log.LogMessage(BbsDiagLevel, " * BbsCopyLocalReference # [N/A - no bbs] " + name + ext);
				}
			}
			return true;
		}

	}
}