using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Mono.Cecil;

namespace BuildBySignature
{
	public class Hasher : IDisposable
	{
		readonly TaskLoggingHelper _log;

		[Conditional("DEBUG")]
		void Log(string message)
		{
//			if (_log != null)
//			{
//				_log.LogMessage(MessageImportance.High, " hasher | " + message);
//			}
#if DEBUG
			_logStream.WriteLine(message);
#endif
		}

#if DEBUG
		string _logName;
		StreamWriter _logStream;
#endif

		Hasher(TaskLoggingHelper log = null, string fileName = null)
		{
			_log = log;
#if DEBUG
			_logName = Path.Combine(Path.GetTempPath(), "hasher " + fileName + ".log");
			_logStream = new StreamWriter(_logName);
#endif
		}

		public int _ctxMembersCount;
		public int _ctxTypesCount;
		bool _isInternalsVisible;

		void Hashin(ref int hash, TypeReference part)
		{
			var hashBy = part.FullName ?? part.ToString();
			Log("TypeReference");
			Hashin(ref hash, hashBy);
			// TODO somebody's generic parameter argument type... like T
		}

		void Hashin(ref int hash, string part)
		{
			Log("String " + part);
			Hashin(ref hash, part.GetHashCode());
		}

		void Hashin(ref int hash, int part)
		{
			unchecked
			{
				hash ^= part * _primeNumbers[_primeNumber++];
				if (_primeNumber >= _primeNumbers.Length)
				{
					_primeNumber = 0;
				}
			}
		}

		int _primeNumber;

		static readonly int[] _primeNumbers = new[]
		{
			281, 283, 293, 307, 311, 313, 317, 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397, 401, 409, 419, 421, 431, 433, 439
		};
		
		public static int Hash(string assemblyFileName, TaskLoggingHelper log = null)
		{
			if (File.Exists(assemblyFileName))
			{
				var asm = AssemblyDefinition.ReadAssembly(assemblyFileName);
				return Hash(asm, log, Path.GetFileName(assemblyFileName));
			}
			else
			{
				return 0;
			}
		}

		public static int Hash(AssemblyDefinition asm, TaskLoggingHelper log = null, string fileName = null)
		{
			int hash = 0;
			using (var h = new Hasher(log, fileName))
			{
				h.Hash(asm, ref hash);
			}
			return hash;
		}

		void Hash(AssemblyDefinition asm, ref int hash)
		{
			// _isInternalsVisible = asm.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false).Any();
			var ivtafn = typeof(InternalsVisibleToAttribute).FullName;
			_isInternalsVisible = asm.CustomAttributes.Any(x => x.AttributeType.FullName == ivtafn);

			// there are some extra checking now... if we omit that we can proceed to public class nested to private... So lets make depth iteration instead of breadth
			// foreach (var type in asm.GetTypes().Where(x => x.IsPublic))
			foreach (var type in asm.Modules.SelectMany(m => m.GetTypes().Where(t => t.IsPublic || (_isInternalsVisible && t.IsNotPublic))))
			{
				Hash(type, ref hash);
			}
			// add module references
			foreach (var reference in asm.Modules.SelectMany(m => m.AssemblyReferences).OrderBy(r => r.FullName))
			{
				Hash(reference, ref hash);
			}
			// Console.WriteLine("0x{0:X8} - {1}", hash, asm.FullName);
		}

		void Hash(AssemblyNameReference reference, ref int hash)
		{
			Hashin(ref hash, reference.FullName);
		}

		void Hash(TypeDefinition type, ref int hash)
		{
			var visibility = type.Attributes & TypeAttributes.VisibilityMask;
			if (IsVisible(visibility) && IsVisible(type))
			{
				_ctxTypesCount++;
				Log("Type " + type.FullName);
				//Console.WriteLine("{0}", type.FullName);

//				foreach (var member in type.meGetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
//				{
//					Hash(member, ref hash);
//				}

				foreach (var methodDefinition in type.Methods)
				{
					Hash(methodDefinition, ref hash);
				}

				foreach (var fieldDefinition in type.Fields)
				{
					Hash(fieldDefinition, ref hash);
				}

				foreach (var nestedType in type.NestedTypes)
				{
					Hash(nestedType, ref hash);
				}

				// todo type generic parameters
			}
		}

#if DEBUG
		/// <summary>
		/// XXXAttributes is flags but first 3 bits is just visibility enum...
		/// </summary>
		static string ToStringNoFlags<T>(T instance)
		{
			return Enum.GetName(typeof(T), instance);
		}
#endif

		void Hash(MethodDefinition member, ref int hash)
		{
			var visibility = member.Attributes & MethodAttributes.MemberAccessMask;
			if (IsVisible(visibility))
			{
				Log("Method " + member.Name);
				// Console.WriteLine("Method: {0,20} \t[{1}]", member.Name, ToStringNoFlags(visibility));
				Hashin(ref hash, member.Name);
				foreach (var param in member.Parameters)
				{
					Hashin(ref hash, param.ParameterType);
				}

				Hashin(ref hash, member.ReturnType);

				// todo parameter attributes (like ref!)
				// todo parameter custom attributes (?)
				// todo generic parameters (!)
				// todo generic parameter constraints (!)
			}
		}

		void Hash(FieldDefinition member, ref int hash)
		{
			var visibility = member.Attributes & FieldAttributes.FieldAccessMask;
			if (IsVisible(visibility))
			{
				Log("Field " + member.Name);
				// Console.WriteLine("Field: {0,20} \t[{1}]", member.Name, ToStringNoFlags(visibility));
				Hashin(ref hash, member.Name);
				Hashin(ref hash, member.FieldType);
			}
		}

		bool IsVisible(TypeAttributes visibility)
		{
			switch (visibility)
			{
// allways visible
				case TypeAttributes.Public: // 1 public
				case TypeAttributes.NestedPublic: // 2 nested public
				case TypeAttributes.NestedFamily: // 4 nested protected
				case TypeAttributes.NestedFamORAssem: // 7 nested protected internal
					return true;
// internal
				case TypeAttributes.NotPublic: // 0 internal
				case TypeAttributes.NestedAssembly: // 5 nested internal
				case TypeAttributes.NestedFamANDAssem: // 6 nested protected & internal
					return _isInternalsVisible;
// private
				case TypeAttributes.NestedPrivate: // 3 nested private
					return false;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		bool IsVisible(MethodAttributes visibility)
		{
			switch (visibility)
			{
				case MethodAttributes.Public:
				case MethodAttributes.Family:
				case MethodAttributes.FamORAssem:
					return true;
				case MethodAttributes.Assembly:
				case MethodAttributes.FamANDAssem:
					return _isInternalsVisible;
				// case MethodAttributes.PrivateScope: == 0 ?
				case MethodAttributes.Private:
					return false;
				default:
					throw new ArgumentOutOfRangeException(nameof(visibility));
			}
		}

		bool IsVisible(FieldAttributes visibility)
		{
			switch (visibility)
			{
				case FieldAttributes.Public:
				case FieldAttributes.Family:
				case FieldAttributes.FamORAssem:
					return true;
				case FieldAttributes.Assembly:
				case FieldAttributes.FamANDAssem:
					return _isInternalsVisible;
				// case FieldAttributes.PrivateScope: == 0?
				case FieldAttributes.Private:
					return false;
				default:
					throw new ArgumentOutOfRangeException("visibility");
			}
		}

		bool IsVisible(TypeDefinition type)
		{
			return !type.FullName.Contains("<PrivateImplementationDetails>");
		}

		public void Dispose()
		{
#if DEBUG
			_logStream.Dispose();
#endif
		}
	}
}
