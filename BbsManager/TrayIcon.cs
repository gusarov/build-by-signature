using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace BbsManager
{
	internal class TrayIcon : IDisposable
	{
		private NotifyIcon _icon;
		private ContextMenu _menu;

		public void InitializeComponents()
		{
			_icon = new NotifyIcon
			        	{
			        		Text = @"Bbs Manager",
							Visible =  true,
							Icon =  Properties.Resources.Icon,
			        	};


			_menu = new ContextMenu();
			_menu.MenuItems.Add(0, new MenuItem("Configuration", Configuration_Click));
			_menu.MenuItems.Add(1, new MenuItem("Hide", Hide_Click));
			_menu.MenuItems.Add(2, new MenuItem("Close", Close_Click));

			_icon.ContextMenu = _menu;
		}

		void Configuration_Click(object sender, EventArgs eventArgs)
		{
			var wnd = System.Windows.Application.Current.MainWindow;
			wnd.Show();
			wnd.Activate();
			wnd.Focus();
			if (wnd.WindowState == WindowState.Minimized)
			{
				wnd.WindowState = WindowState.Normal;
			}
			wnd.Activate();
			wnd.Focus();
		}

		void Close_Click(object sender, EventArgs eventArgs)
		{
			System.Windows.Application.Current.Shutdown();
		}

		void Hide_Click(object sender, EventArgs eventArgs)
		{
			System.Windows.Application.Current.MainWindow.WindowState = WindowState.Minimized;
		}

		public void Dispose()
		{
			_icon.Dispose();
		}
	}
}
