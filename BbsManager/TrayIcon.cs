using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BbsManager
{
	internal class TrayIcon : IDisposable
	{
		private NotifyIcon _icon;
		private ContextMenu _menu;
		MenuItem _defaultMi;
		MenuItem _enableMi;
		MenuItem _disableMi;
		MenuItem _enableLmMi;
		MenuItem _disableLmMi;
		MenuItem _system;

		public void InitializeComponents()
		{
			_icon = new NotifyIcon
			        	{
			        		Text = @"Bbs Manager",
							Visible =  true,
							Icon =  Properties.Resources.Icon,
			        	};


			_menu = new ContextMenu();
			_system.MenuItems.Add(_defaultMi = new MenuItem("CurrentUser: Default ({0})", Default_Click));
			_system.MenuItems.Add(new MenuItem("-"));
			_menu.MenuItems.Add(_enableMi = new MenuItem("Enable", Enable_Click));
			_menu.MenuItems.Add(_disableMi = new MenuItem("Disable", Disable_Click));
			_menu.MenuItems.Add(new MenuItem("-"));
			_system = new MenuItem("Options");
			_menu.MenuItems.Add(_system);
			_enableLmMi = _system.MenuItems.Add("System-wide: Enable");
			_disableLmMi = _system.MenuItems.Add("System-wide: Disable");
			_system.MenuItems.Add(new MenuItem("-"));
			_system.MenuItems.Add(new MenuItem("Close", Close_Click));
			_menu.Popup += new EventHandler(_menu_Popup);
			_system.Popup += new EventHandler(system_Popup);

			_icon.ContextMenu = _menu;
		}

		void system_Popup(object sender, EventArgs e)
		{
			foreach (MenuItem item in _system.MenuItems)
			{
				item.Checked = false;
				item.Enabled = true;
			}
			if (_vm.IsEnabled == null)
			{
				_defaultMi.Checked = true;
				_defaultMi.Enabled = false;
			}
			if (_vm.IsEnabledLocalMachine)
			{
				_enableLmMi.Checked = true;
				_enableLmMi.Enabled = false;
			}
			else
			{
				_disableLmMi.Checked = true;
				_disableLmMi.Enabled = false;
			}
		}

		TrayIconViewModel _vm = new TrayIconViewModel();

		void _menu_Popup(object sender, EventArgs e)
		{
			foreach (MenuItem item in _menu.MenuItems)
			{
				item.Checked = false;
				item.Enabled = true;
			}
			switch (_vm.IsEnabled)
			{
				case true:
					_enableMi.Checked = true;
					_enableMi.Enabled = false;
					break;
				case false:
					_disableMi.Checked = true;
					_disableMi.Enabled = false;
					break;
				default:
					_defaultMi.Checked = true;
					_defaultMi.Enabled = false;
					break;
			}
			_defaultMi.Text = string.Format("CurrentUser: Default ({0})", _vm.IsEnabledLocalMachine ? "Enabled" : "Disabled");
		}

		void Default_Click(object sender, EventArgs eventArgs)
		{
			_vm.IsEnabled = null;
		}

		void Enable_Click(object sender, EventArgs eventArgs)
		{
			_vm.IsEnabled = true;
		}

		void Disable_Click(object sender, EventArgs eventArgs)
		{
			_vm.IsEnabled = false;
		}

		void Close_Click(object sender, EventArgs eventArgs)
		{
			System.Windows.Application.Current.Shutdown();
		}

		public void Dispose()
		{
			_icon.Dispose();
		}
	}

	class TrayIconViewModel
	{
		public bool? IsEnabled
		{
			get
			{
				var value = (string)Registry.CurrentUser.GetValue(@"SOFTWARE\Bbs\Enabled", null);
				return string.IsNullOrEmpty(value) ? default(bool?) : bool.Parse(value);
			}
			set
			{
				Registry.CurrentUser.SetValue(@"SOFTWARE\Bbs\Enabled", value == null ? string.Empty : value.ToString());
			}
		}

		public bool IsEnabledLocalMachine
		{
			get
			{
				var value = (string)Registry.LocalMachine.GetValue(@"SOFTWARE\Bbs\Enabled", null);
				return string.IsNullOrEmpty(value) ? default(bool) : bool.Parse(value);
			}
			set
			{
				Registry.LocalMachine.SetValue(@"SOFTWARE\Bbs\Enabled", value.ToString());
			}
		}
	}
}
