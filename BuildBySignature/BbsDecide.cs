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
			Console.WriteLine("=> Generating truncated references list...");
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
						Console.WriteLine("=> Hash = " + hash);
						var bssLocalFileName = Path.Combine(IntermediateOutputPath, Path.GetFileName(bssFileName));
						if (File.Exists(bssLocalFileName))
						{
							var hashOfPreviousCompilation = File.ReadAllText(bssLocalFileName);
							Console.WriteLine("=> Local hash = " + hashOfPreviousCompilation);
							if (hashOfPreviousCompilation == hash)
							{
								continue; // do not mark it alive
							}
						}
						else
						{
							Console.WriteLine("=> but we have no local hash");
						}
					}
				}
				referencesAlive.Add(reference);
			}

			ReferencesAlive = referencesAlive.ToArray();

			return true;
		}
	}
}
