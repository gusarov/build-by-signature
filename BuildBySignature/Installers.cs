using System.Linq;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace BuildBySignature
{
	[RunInstaller(true)]
	public class PatchMSBuildTargets : System.Configuration.Install.Installer
	{
		string _tragetDir;

		public override void Install(IDictionary stateSaver)
		{
			base.Install(stateSaver);
			stateSaver.Add("TargetDir", _tragetDir = Context.Parameters["TargetDir"]); 
			ProcessFiles(true);
		}

		public override void Uninstall(IDictionary savedState)
		{
			base.Uninstall(savedState);
			ProcessFiles(false);
		}

		IEnumerable<string> MsBuildTargetsToProcess(bool afterOrBefore)
		{
			// yield return GetMsBuildTargetsPath("v2.0", afterOrBefore);
			//yield return GetMsBuildTargetsPath("v3.5", afterOrBefore);
			yield return GetMsBuildTargetsPath("v4.0", afterOrBefore);
		}

		string GetMsBuildTargetsPath(string ver, bool afterOrBefore)
		{
			var p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MSBuild");
			var pVer = Path.Combine(p, ver);
			return Path.Combine(pVer, string.Format("Custom.{0}.Microsoft.Common.targets", afterOrBefore ? "After" : "Before"));
		}

		void ProcessFile(string fileName, bool appendOrRemoveOnly, bool afterOrBefore)
		{
			var content = "<Project xmlns='http://schemas.microsoft.com/developer/msbuild/2003'></Project>";
			var dir = Path.GetDirectoryName(fileName);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			if (File.Exists(fileName))
			{
				content = File.ReadAllText(fileName);
			}

			File.WriteAllText(fileName, CustomAfterMicrosoftCommonCore(content, appendOrRemoveOnly, afterOrBefore));
		}

		static string GetTargetsName(bool afterOrBefore)
		{
			return afterOrBefore
				? "bbs.targets"
				: "bbs_before.targets";
		}

		string CustomAfterMicrosoftCommonCore(string fileContent, bool appendOrRemoveOnly, bool afterOrBefore)
		{
			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(fileContent);
			var nsman = new XmlNamespaceManager(xmlDoc.NameTable);
			var ns = "http://schemas.microsoft.com/developer/msbuild/2003";
			nsman.AddNamespace("x", ns);

			var root = xmlDoc.SelectSingleNode("x:Project", nsman);

			// remove old
			var arr = root.SelectNodes(string.Format("x:Import[contains(@Project,'{0}')]", GetTargetsName(afterOrBefore)), nsman);
			if (arr != null)
			{
				foreach (XmlNode item in arr)
				{
					root.RemoveChild(item);
				}
			}

			// add new
			if (appendOrRemoveOnly)
			{
				var import = xmlDoc.CreateElement("Import", ns);

				var pathToTargets = Path.Combine(_tragetDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), GetTargetsName(afterOrBefore));

				import.SetAttribute("Project", pathToTargets);
				import.SetAttribute("Condition", @"Exists('" + pathToTargets + "')");

				root.AppendChild(import);
			}

			using (var ms = new MemoryStream())
			{
				using (var tw = new StreamWriter(ms))
				{
					using (var writer = new XmlTextWriter(tw))
					{
						writer.Indentation = 1;
						writer.IndentChar = '\t';
						writer.Formatting = Formatting.Indented;
						xmlDoc.Save(writer);
					}
				}
				return Encoding.UTF8.GetString(ms.ToArray());
			}

		}

		void ProcessFiles(bool appendOrRemoveOnly)
		{
//			foreach (var item in MsBuildTargetsToProcess(false))
//			{
//				ProcessFile(item, appendOrRemoveOnly, false);
//			}
			foreach (var item in MsBuildTargetsToProcess(true))
			{
				ProcessFile(item, appendOrRemoveOnly, true);
			}
		}
	}
}