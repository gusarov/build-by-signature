using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;

namespace MyUtils.UAC
{
	static class Program
	{
		static int Main(string[] args)
		{
			try
			{
				if (args.Length != 1)
				{
					Console.WriteLine("CommandLine: MyUtils.UAC <port>");
					return 1;
				}

				var port = int.Parse(args[0], CultureInfo.InvariantCulture);

				var listener = new TcpListener(IPAddress.Loopback, port);
				listener.Start();
				var client = listener.AcceptTcpClient();
				while (client.Connected)
				{
					var msg = client.GetStream().ReceivePackage().Utf8();
					var classNameSeparator = msg.IndexOf('*');
					var className = msg.Substring(0, classNameSeparator);
					var arguments = msg.Substring(classNameSeparator + 1);
					byte[] ret;
					try
					{
						ret = new byte[] {0}.Concat(Elevation.Instance.Perform(className, arguments).Utf8()).ToArray();
					}
					catch(Exception ex)
					{
						var ms = new MemoryStream();
						_exceptionFormatter.Value.Serialize(ms, ex);
						ret = new byte[] {1}.Concat(ms.ToArray()).ToArray();
					}
					client.GetStream().SendPackage(ret);
				}
			}
			catch (SocketException ex)
			{
				return 2;
			}
			catch (IOException ex)
			{
				return 3;
			}
			catch (Exception ex)
			{
#if DEBUG
				MessageBox.Show(ex.ToString(), "Debug: My Remoting Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
				return 4;
			}
			return 0;
		}

		static readonly Lazy<BinaryFormatter> _exceptionFormatter = new Lazy<BinaryFormatter>(()=>new BinaryFormatter());
	}
}
