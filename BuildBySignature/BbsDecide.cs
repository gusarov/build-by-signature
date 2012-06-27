using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BuildBySignature
{
	public class BbsDecide : Task
	{
		string _bbsDiagLevel;
		MessageImportance _bbsDiagLevelEnum;

		[Required]
		public string BbsDiagLevel
		{
			get { return _bbsDiagLevel; }
			set
			{
				_bbsDiagLevel = value;
				_bbsDiagLevelEnum = (MessageImportance) Enum.Parse(typeof(MessageImportance), value);
			}
		}

		[Required]
		public ITaskItem[] References { get; set; }

		[Required]
		public string IntermediateOutputPath { get; set; }

		[Output]
		public ITaskItem[] ReferencesAlive { get; set; }

		public override bool Execute()
		{
			Log.LogMessage(BbsDiagLevel, "=> Generating truncated references list...");
			var referencesAlive = new List<ITaskItem>();
			foreach (var reference in References)
			{
				var spec = Path.GetFullPath(reference.ItemSpec);
				if (File.Exists(spec))
				{
					var bssFileName = Path.Combine(Path.GetDirectoryName(spec), Path.GetFileNameWithoutExtension(spec) + ".bbs");
					if (File.Exists(bssFileName))
					{
						var hash = File.ReadAllText(bssFileName);
						Log.LogMessage(_bbsDiagLevelEnum, "=> Hash = " + hash);
						var bssLocalFileName = Path.Combine(IntermediateOutputPath, Path.GetFileName(bssFileName));
						if (File.Exists(bssLocalFileName))
						{
							var hashOfPreviousCompilation = File.ReadAllText(bssLocalFileName);
							Log.LogMessage(BbsDiagLevel, "=> Local hash = " + hashOfPreviousCompilation);
							if (hashOfPreviousCompilation == hash)
							{
								Log.LogMessage(_bbsDiagLevelEnum, "=> Ignore " + reference.ItemSpec);
								continue; // do not mark it alive
							}
						}
						else
						{
							Log.LogMessage(BbsDiagLevel, "=> but we have no local hash");
						}
					}
				}
				Log.LogMessage(_bbsDiagLevelEnum, "=> Add " + reference.ItemSpec);
				referencesAlive.Add(reference);
			}

			ReferencesAlive = referencesAlive.ToArray();

			foreach (var item in ReferencesAlive)
			{
				Log.LogMessage(_bbsDiagLevelEnum, "=> Result " + item.ItemSpec);
			}

			return true;
		}
	}
}
