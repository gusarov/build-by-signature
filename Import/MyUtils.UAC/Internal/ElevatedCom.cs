using System.Collections.Generic;
using System.Linq;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace MyUtils.UAC.Internal
{
	[ComVisible(true)]
	[Guid(ElevatedComApi.ComIfaceIdString)]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	public interface IElevatedComApi
	{
        string GetIsElevated();
        string TestAdmin();
	}

	[ComImport]
	[Guid(ElevatedComApi.ComIfaceIdString)]
	[InterfaceType(ComInterfaceType.InterfaceIsDual)]
	public interface IElevatedComApiInterop
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig]
        string GetIsElevated();

		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[PreserveSig]
        string TestAdmin();
	}

	[ComVisible(true)]
	[Guid(ComClassIdString)]
	[ClassInterface(ClassInterfaceType.None)]
	public class ElevatedComApi : IElevatedComApi
	{
		public const string ComIfaceIdString = "912502A9-C549-42CE-AFDA-097697B0156C";
		public static readonly Guid ComIfaceId = new Guid(ComIfaceIdString);

		public const string ComClassIdString = "990B6837-8AFC-4C5E-B24A-5F34C39669DE";
		public static readonly Guid ComClassId = new Guid(ComClassIdString);

        public string GetIsElevated()
        {
            try
            {
                return Elevation.IsElevated.ToString();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public string TestAdmin()
        {
            try
            {
                Registry.SetValue("HKEY_LOCAL_MACHINE", "test", "test");
                return "OK! No Problem with recording to HKLM";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

	    public object CreateInstance(Type type)
		{
			return Activator.CreateInstance(type);
		}

		public object CreateInstance(string typeName)
		{
			return Activator.CreateInstance(Type.GetType(typeName, true));
		}

		public object CreateInstance<T>()
		{
			return Activator.CreateInstance<T>();
		}
	}

}