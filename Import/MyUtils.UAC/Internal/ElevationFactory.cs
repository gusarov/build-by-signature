using System.Collections.Generic;
using System.Linq;
using System;
using System.Runtime.InteropServices;

namespace MyUtils.UAC.Internal
{
	public static class ElevationFactory
	{
		[StructLayout(LayoutKind.Sequential)]
		struct BIND_OPTS3
		{
			public uint cbStruct;
			uint grfFlags;
			uint grfMode;
			uint dwTickCountDeadline;
			uint dwTrackFlags;
			public uint dwClassContext;
			uint locale;
			object pServerInfo; // will be passing null, so type doesn't matter
			public IntPtr hwnd;
		}

		[DllImport("ole32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
		[return: MarshalAs(UnmanagedType.Interface)]
		static extern object CoGetObject(
			string pszName,
			[In] ref BIND_OPTS3 pBindOptions,
			[In] [MarshalAs(UnmanagedType.LPStruct)] Guid riid);

		[return: MarshalAs(UnmanagedType.Interface)]
		static object LaunchElevatedComObject(IntPtr parentWindow, Guid clsid, Guid interfaceId)
		{
			var clsidFormatted = clsid.ToString("B"); // B formatting directive: returns {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx} 
			var monikerName = "Elevation:Administrator!new:" + clsidFormatted;

			var bo = new BIND_OPTS3();
			bo.cbStruct = (uint)Marshal.SizeOf(bo);
			bo.hwnd = parentWindow;
			bo.dwClassContext = 4; // CLSCTX_LOCAL_SERVER

			var obj = CoGetObject(monikerName, ref bo, interfaceId);
			_coms.AddLast(obj);
			return obj;
		}

		[return: MarshalAs(UnmanagedType.Interface)]
		static object LaunchElevatedComObject(IntPtr parentWindow = default(IntPtr))
		{
			return LaunchElevatedComObject(parentWindow, ElevatedComApi.ComClassId, ElevatedComApi.ComIfaceId);
		}

		public static IElevatedComApi LaunchElevatedComObjectApi(IntPtr parentWindow = default(IntPtr))
		{
			return (IElevatedComApi)LaunchElevatedComObject(parentWindow);
		}

		public static void ReleaseAllComObject()
		{
			foreach (var com in _coms)
			{
				try
				{
					Marshal.ReleaseComObject(com);
				}
				catch {}
			}
			_coms.Clear();
		}

		static readonly LinkedList<object> _coms = new LinkedList<object>();
	}
}