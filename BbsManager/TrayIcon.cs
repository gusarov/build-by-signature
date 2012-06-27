using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;
using MyUtils.UAC;

namespace BbsManager
{
	internal class TrayIcon : IDisposable
	{
		private NotifyIcon _icon;
		private ContextMenu _menu;
		MenuItem _defaultMi;
		MenuItem _defaultMi2;
		MenuItem _enableMi;
		MenuItem _disableMi;
		MenuItem _enableLmMi;
		MenuItem _disableLmMi;
		MenuItem _system;
		MenuItem _diagnosticMi;

		public void InitializeComponents()
		{
			_icon = new NotifyIcon
			{
				Text = @"Bbs Manager",
				Visible = true,
				Icon = Properties.Resources.Icon,
			};

			_menu = new ContextMenu();
			_menu.MenuItems.Add(_defaultMi = new MenuItem("Default ({0})", Default_Click));
			_menu.MenuItems.Add(_enableMi = new MenuItem("Enable", Enable_Click));
			_menu.MenuItems.Add(_disableMi = new MenuItem("Disable", Disable_Click));
			_menu.MenuItems.Add(new MenuItem("-"));
			_system = new MenuItem("Options");
			_menu.MenuItems.Add(_system);
			_system.MenuItems.Add(_diagnosticMi = new MenuItem("Diagnostic Logging", Diagnostic_Click));
			_system.MenuItems.Add(_defaultMi2 = new MenuItem("CurrentUser: Default ({0})", Default_Click));
			_system.MenuItems.Add(new MenuItem("-"));
			_enableLmMi = _system.MenuItems.Add("System-wide: Enable", SwEnable_Click);
			_disableLmMi = _system.MenuItems.Add("System-wide: Disable", SwDisable_Click);
			_system.MenuItems.Add(new MenuItem("-"));
			_system.MenuItems.Add(new MenuItem("Close", Close_Click));
			_menu.Popup += _menu_Popup;
			_system.Popup += system_Popup;

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
				_defaultMi2.Checked = true;
				_defaultMi2.Enabled = false;
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
			_diagnosticMi.Checked = _vm.IsDiagnostic;
		}

		readonly TrayIconViewModel _vm = new TrayIconViewModel();

		void _menu_Popup(object sender, EventArgs e)
		{
			foreach (MenuItem item in _menu.MenuItems)
			{
				item.Checked = false;
				item.Enabled = true;
			}
			_defaultMi.Visible = false;
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
					_defaultMi.Visible = true;
					_defaultMi2.Checked = true;
					_defaultMi2.Enabled = false;
					break;
			}
			_defaultMi.Text = string.Format("Default ({0})", _vm.IsEnabledLocalMachine ? "Enabled" : "Disabled");
			_defaultMi2.Text = string.Format("CurrentUser: Default ({0})", _vm.IsEnabledLocalMachine ? "Enabled" : "Disabled");
		}

		public class UacCall : IElevationCall
		{
			public string Call(string args)
			{
				var vm = new TrayIconViewModel();
				switch (args)
				{
					case "SwEnable":
						vm.IsEnabledLocalMachine = true;
						break;
					case "SwDisable":
						vm.IsEnabledLocalMachine = false;
						break;
				}
				return null;
			}
		}

		void Diagnostic_Click(object sender, EventArgs eventArgs)
		{
			_vm.IsDiagnostic = !_vm.IsDiagnostic;
		}

		void SwEnable_Click(object sender, EventArgs eventArgs)
		{
			Elevation.Instance.OneTimePerProcess<UacCall>("SwEnable", false);
		}

		void SwDisable_Click(object sender, EventArgs eventArgs)
		{
			Elevation.Instance.OneTimePerProcess<UacCall>("SwDisable", false);
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

		internal void FirstBalloon()
		{
			_icon.ShowBalloonTip(15000, "Build by Signature", "Here you can turn me on and off", ToolTipIcon.Info);
		}
	}

	class TrayIconViewModel
	{
		public bool? IsEnabled
		{
			get
			{
				var value = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Bbs", "Enabled", null);
				return string.IsNullOrEmpty(value) ? default(bool?) : bool.Parse(value);
			}
			set
			{
				Registry.SetValue(@"HKEY_CURRENT_USER\Software\Bbs", "Enabled", value == null ? string.Empty : value.Value.ToString(CultureInfo.InvariantCulture));
				Registry.SetValue(@"HKEY_CURRENT_USER\Software\Wow6432Node\Bbs", "Enabled", value == null ? string.Empty : value.Value.ToString(CultureInfo.InvariantCulture));
			}
		}

		public bool IsEnabledLocalMachine
		{
			get
			{
				var value = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Bbs", "Enabled", null);
				return string.IsNullOrEmpty(value) ? default(bool) : bool.Parse(value);
			}
			set
			{
				Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Bbs", "Enabled", value.ToString(CultureInfo.InvariantCulture));
				Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Wow6432Node\Bbs", "Enabled", value.ToString(CultureInfo.InvariantCulture));
			}
		}

		public bool IsDiagnostic
		{
			get
			{
				var value = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Bbs", "Diagnostic", null);
				return string.IsNullOrEmpty(value) ? default(bool) : bool.Parse(value);
			}
			set
			{
				Registry.SetValue(@"HKEY_CURRENT_USER\Software\Bbs", "Diagnostic", value.ToString(CultureInfo.InvariantCulture));
			}
		}
	}
}
