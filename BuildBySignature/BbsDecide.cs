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
		[Required]
		public ITaskItem[] References { get; set; }

		[Required]
		public string IntermediateOutputPath { get; set; }

		[Output]
		public ITaskItem[] ReferencesAlive { get; set; }

		public override bool Execute()
		{
			Log.LogMessage(MessageImportance.High, "=> Generating truncated references list...");
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
						Log.LogMessage(MessageImportance.High, "=> Hash = " + hash);
						var bssLocalFileName = Path.Combine(IntermediateOutputPath, Path.GetFileName(bssFileName));
						if (File.Exists(bssLocalFileName))
						{
							var hashOfPreviousCompilation = File.ReadAllText(bssLocalFileName);
							Log.LogMessage(MessageImportance.High, "=> Local hash = " + hashOfPreviousCompilation);
							if (hashOfPreviousCompilation == hash)
							{
								Log.LogMessage(MessageImportance.High, "=> Ignore " + reference.ItemSpec);
								continue; // do not mark it alive
							}
						}
						else
						{
							Log.LogMessage(MessageImportance.High, "=> but we have no local hash");
						}
					}
				}
				Log.LogMessage(MessageImportance.High, "=> Add " + reference.ItemSpec);
				referencesAlive.Add(reference);
			}

			ReferencesAlive = referencesAlive.ToArray();

			foreach (var item in ReferencesAlive)
			{
				Log.LogMessage(MessageImportance.High, "=> Result " + item.ItemSpec);
			}

			return true;
		}
	}
}
