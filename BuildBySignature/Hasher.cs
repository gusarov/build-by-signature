using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.CompilerServices;

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

		static void Hashin(ref int hash, Type part)
		{
			Hashin(ref hash, part.AssemblyQualifiedName ?? part.FullName ?? part.ToString());
			// TODO somebody's generic parameter argument type... like T
		}

		static void Hashin(ref int hash, object part)
		{
			Hashin(ref hash, part.GetHashCode());
		}

		static void Hashin(ref int hash, int part)
		{
			unchecked
			{
				hash ^= part * 397;
			}
		}

		public static int Hash(Assembly asm)
		{
			int hash = 0;
			new Hasher().Hash(asm, ref hash);
			return hash;
		}

		void Hash(Assembly asm, ref int hash)
		{
			_isInternalsVisible = asm.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false).Any();

			foreach (var type in asm.GetTypes().Where(x=>x.IsPublic)) // there are some extra checking now... if we omit that we can proceed to public class nested to private... So lets make depth iteration instead of breadth
			{
				Hash(type, ref hash);
			}
			// Console.WriteLine("0x{0:X8} - {1}", hash, asm.FullName);
		}

		void Hash(Type type, ref int hash)
		{
			var visibility = type.Attributes & TypeAttributes.VisibilityMask;
			if (IsVisible(visibility))
			{
				_ctxTypesCount++;
				Console.WriteLine("{0}", type.FullName);

				foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				{
					Hash(member, ref hash);
				}
			}
		}

		void Hash(MemberInfo info, ref int hash)
		{
			switch (info.MemberType)
			{
				case MemberTypes.Constructor:
				case MemberTypes.Method:
					_ctxMembersCount++;
					Hash((MethodBase)info, ref hash);
					return;
				case MemberTypes.Field:
					_ctxMembersCount++;
					Hash((FieldInfo)info, ref hash);
					return;
				case MemberTypes.Event:
				case MemberTypes.Property:
					// already hashed by accessors
					return;
				case MemberTypes.NestedType:
					Hash((Type)info, ref hash);
					return;
				case MemberTypes.TypeInfo:
				case MemberTypes.Custom:
				case MemberTypes.All:
				default:
					throw new NotSupportedException("MemberType = " + info.MemberType.ToString());
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

		void Hash(MethodBase member, ref int hash)
		{
			var visibility = member.Attributes & MethodAttributes.MemberAccessMask;
			if (IsVisible(visibility))
			{
				// Console.WriteLine("Method: {0,20} \t[{1}]", member.Name, ToStringNoFlags(visibility));
				Hashin(ref hash, member.Name);
				foreach (var param in member.GetParameters())
				{
					Hashin(ref hash, param.ParameterType);
				}

				var mi = member as MethodInfo; // not a constructor
				if (mi != null)
				{
					Hashin(ref hash, mi.ReturnType);
				}

				// todo parameter attributes (like ref!)
				// todo parameter custom attributes (?)
				// todo generic parameters (!)
				// todo generic parameter constraints (!)
			}
		}

		// static Random _rnd = new Random();

		void Hash(FieldInfo member, ref int hash)
		{
			var visibility = member.Attributes & FieldAttributes.FieldAccessMask;
			if (IsVisible(visibility))
			{
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
				case MethodAttributes.PrivateScope:
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
				case FieldAttributes.PrivateScope:
				case FieldAttributes.Private:
					return false;
				default:
					throw new ArgumentOutOfRangeException("visibility");
			}
		}
	}
}
