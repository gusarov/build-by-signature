using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Mono.Cecil;

using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace BuildBySignature
{
	public class Hasher
	{
		Hasher()
		{

		}

		public int _ctxMembersCount;
		public int _ctxTypesCount;
		bool _isInternalsVisible;

//		void Hashin(ref int hash, TypeDefinition part)
//		{
//			Hashin(ref hash, part.AssemblyQualifiedName ?? part.FullName ?? part.ToString());
//			// TODO somebody's generic parameter argument type... like T
//		}

		void Hashin(ref int hash, object part)
		{
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
		
		public static int Hash(string assemblyFileName)
		{
			var asm = AssemblyDefinition.ReadAssembly(assemblyFileName);
			return Hash(asm);
		}

		public static int Hash(AssemblyDefinition asm)
		{
			int hash = 0;
			new Hasher().Hash(asm, ref hash);
			return hash;
		}

		void Hash(AssemblyDefinition asm, ref int hash)
		{
			// _isInternalsVisible = asm.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false).Any();
			var ivtafn = typeof(InternalsVisibleToAttribute).FullName;
			_isInternalsVisible = asm.CustomAttributes.Any(x => x.AttributeType.FullName == ivtafn);

			// there are some extra checking now... if we omit that we can proceed to public class nested to private... So lets make depth iteration instead of breadth
			// foreach (var type in asm.GetTypes().Where(x => x.IsPublic))
			foreach (var type in asm.Modules.SelectMany(m=>m.GetTypes().Where(t=>t.IsPublic || (_isInternalsVisible && t.IsNotPublic))))
			{
				Hash(type, ref hash);
			}
			// Console.WriteLine("0x{0:X8} - {1}", hash, asm.FullName);
		}

		void Hash(TypeDefinition type, ref int hash)
		{
			var visibility = type.Attributes & TypeAttributes.VisibilityMask;
			if (IsVisible(visibility))
			{
				_ctxTypesCount++;
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
					throw new ArgumentOutOfRangeException("visibility");
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
	}
}
