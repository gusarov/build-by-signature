using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace MyUtils.UAC
{
//	public class OneTimeElevation
//	{
//		OneTimeElevation()
//		{
//			
//		}
//
//		readonly static OneTimeElevation _instance = new OneTimeElevation();
//		public static OneTimeElevation Instance { get { return _instance; } }
//
//	}

	public sealed class Elevation : INotifyPropertyChanged
	{
		Elevation()
		{
			
		}

		static readonly Elevation _instance = new Elevation();

		public static Elevation Instance
		{
			get { return _instance; }
		}

		readonly Lazy<bool> _isElevated = new Lazy<bool>(() => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator));

		/// <summary>
		/// Determine whether current WindowsPrincipal have Administrator role
		/// </summary>
		public bool IsElevated
		{
			get { return _isElevated.Value; }
		}

		public bool IsElevatedOneTimePerProcess
		{
			get { return _elevatedProcess != null && !_elevatedProcess.HasExited && _elevatedProcessClient.Connected; }
		}

		public bool IsElevatedOrOneTimePerProcess
		{
			get { return IsElevated || IsElevatedOneTimePerProcess; }
		}

		/// <summary>
		/// Elevate only once per process
		/// </summary>
		public string OneTimePerProcess<T>(string args = null, bool throwOnCancel = true, IntPtr parentWindowHandle = default(IntPtr)) where T : IElevationCall
		{
			if (IsElevated)
			{
				return Perform<T>(args);
			}

			if (!IsElevatedOneTimePerProcess)
			{
				_port = _rnd.Next(ushort.MaxValue);
				try
				{
					_elevatedProcess = ElevateProcess(parentWindowHandle, Assembly.GetExecutingAssembly().Location, _port.ToString());
				}
				catch (Win32Exception ex)
				{
					if (ex.ErrorCode == -2147467259) // The operation was canceled by the user
					{
						if (throwOnCancel)
						{
							throw;
						}
						return null;
					}
				}
				_elevatedProcessClient = new TcpClient();
				const int max = 5;
				for (int i = 0; i < max; i++)
				{
					try
					{
						_elevatedProcessClient.Connect(new IPEndPoint(IPAddress.Loopback, _port));
						break;
					}
					catch (Exception ex)
					{
						if (i >= max)
						{
							throw;
						}
						Thread.Sleep(1000);
					}
				}
				OnPropertyChanged(null);
			}

			_elevatedProcessClient.GetStream().SendPackage((typeof(T).AssemblyQualifiedName + "*" + args).Utf8());
			var ret = _elevatedProcessClient.GetStream().ReceivePackage();
			var ms = new MemoryStream(ret);

			var exception = ms.ReadByte() > 0;
			if (exception)
			{
				throw (Exception)_exceptionFormatter.Value.Deserialize(ms);
			}

			return new StreamReader(ms).ReadToEnd();
		}

		readonly Lazy<BinaryFormatter> _exceptionFormatter = new Lazy<BinaryFormatter>(() => new BinaryFormatter());

		Process _elevatedProcess;
		TcpClient _elevatedProcessClient;
		int _port;
		readonly Random _rnd = new Random();

		readonly Dictionary<Type, IElevationCall> _elevatedClasses = new Dictionary<Type, IElevationCall>();
//
//		class TypeEquality : IEqualityComparer<Type>
//		{
//			static readonly TypeEquality _instance = new TypeEquality();
//
//			public static TypeEquality Instance
//			{
//				get { return _instance; }
//			}
//
//			TypeEquality()
//			{
//				
//			}
//
//			public bool Equals(Type x, Type y)
//			{
//				return x.AssemblyQualifiedName.Equals(y.AssemblyQualifiedName, StringComparison.InvariantCultureIgnoreCase);
//			}
//
//			public int GetHashCode(Type obj)
//			{
//				return obj.AssemblyQualifiedName.GetHashCode();
//			}
//		}

		internal string Perform(string typeName, string args)
		{
			return Perform(Type.GetType(typeName, true), args);
		}

		internal string Perform<T>(string args)
		{
			return Perform(typeof(T), args);
		}

		internal string Perform(Type type, string args)
		{
			IElevationCall call;
			if (!_elevatedClasses.TryGetValue(type, out call))
			{
				_elevatedClasses[type] = call = (IElevationCall)Activator.CreateInstance(type, true);
			}
			return call.Call(args ?? string.Empty) ?? string.Empty; // allways pass empty string to and from - like after network serialization
		}

		/// <summary>
		/// Run specified delegate only when elevated. Otherwise - ElevateThisProcess
		/// </summary>
		/// <param name="act">delegate for elevated execution</param>
		public void EnsureElevatedProcess(Action act)
		{
			if (IsElevated)
			{
				act();
			}
			else
			{
				ElevateThisProcess();
			}
		}

		/// <summary>
		/// Restart this process in elevated mode. Current main WPF Window used.
		/// </summary>
		public void ElevateThisProcess()
		{
			var app = Application.Current;
			Window window = null;
			if (app != null)
			{
				window = app.MainWindow;
			}
			ElevateThisProcess(window);
		}

		public void ElevateThisProcess(Window parentWindow)
		{
			var hWnd = default(IntPtr);
			if (parentWindow != null)
			{
				hWnd = new WindowInteropHelper(parentWindow).Handle;
			}
			ElevateThisProcess(hWnd);
		}

		public void ElevateThisProcess(IntPtr parentWindowHandle)
		{
			var exe = Assembly.GetEntryAssembly();
			if (exe == null)
			{
				throw new Exception("EntryAssembly is not specified (May be it is UnitTests or Another AppDomain?)");
			}
			var p = ElevateProcess(parentWindowHandle, exe.Location);
			if (p != null)
			{
				var app = Application.Current;
				if (app != null)
				{
					app.Shutdown();
				}
				else
				{
					System.Windows.Forms.Application.Exit();
				}
			}
		}

		public Process ElevateProcess(IntPtr parentWindowHandle, string fileName, string fileArgs = null)
		{
			if (IsElevated)
			{
				return null;
			}

			var psi = new ProcessStartInfo(fileName, fileArgs)
			{
				UseShellExecute = true,
				ErrorDialog = true,
				ErrorDialogParentHandle = parentWindowHandle,
				Verb = "runas",
			};

			return Process.Start(psi);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string name)
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(name));
			}
		}
	}
}
