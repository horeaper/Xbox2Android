using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Managed.Adb;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;

namespace XboxInputMapper
{
	partial class MainWindow
	{
		NotifyIcon m_notifyIcon;
		ContextMenu m_iconMenu;

		MenuItem m_menuDeviceSelect;
		MenuItem m_menuTriggerMode;
		MenuItem m_menuReverseAxis;
		MenuItem m_menuExit;

		void InitializeNotifyIcon()
		{
			m_menuDeviceSelect = new MenuItem("Device Select");
			m_menuTriggerMode = new MenuItem("Trigger Mode");
			foreach (ComboBoxItem item in comboTriggerMode.Items) {
				var menuItem = new MenuItem(item.Content.ToString());
				menuItem.Click += MenuTriggerMode_Click;
				m_menuTriggerMode.MenuItems.Add(menuItem);
			}
			m_menuReverseAxis = new MenuItem("Reverse Axis");
			m_menuReverseAxis.Click += MenuReverseAxis_Click;
			m_menuExit = new MenuItem("Exit");
			m_menuExit.Click += MenuExit_Click;

			m_iconMenu = new ContextMenu();
			m_iconMenu.Popup += ContextMenu_Popup;
			m_iconMenu.MenuItems.AddRange(new[] { m_menuDeviceSelect, m_menuTriggerMode, m_menuReverseAxis, new MenuItem("-"), m_menuExit });

			m_notifyIcon = new NotifyIcon();
			m_notifyIcon.Icon = Properties.Resources.Program;
			m_notifyIcon.Visible = false;
			m_notifyIcon.Text = "Xbox 2 Android";
			m_notifyIcon.ContextMenu = m_iconMenu;
			m_notifyIcon.MouseClick += NotifyIcon_MouseClick;
		}

		private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left) {
				Show();
				WindowState = WindowState.Normal;
				m_notifyIcon.Visible = false;
			}
		}

		private void ContextMenu_Popup(object sender, EventArgs e)
		{
			m_menuDeviceSelect.MenuItems.Clear();
			foreach (ComboBoxItem item in comboDevices.Items) {
				var menuItem = new MenuItem(item.Content.ToString());
				menuItem.Click += MenuDeviceSelect_Click;
				m_menuDeviceSelect.MenuItems.Add(menuItem);
			}
			foreach (MenuItem item in m_menuTriggerMode.MenuItems) {
				item.Checked = false;
			}

			if (comboDevices.SelectedIndex != -1) {
				m_menuDeviceSelect.MenuItems[comboDevices.SelectedIndex].Checked = true;
			}
			if (comboTriggerMode.SelectedIndex != -1) {
				m_menuTriggerMode.MenuItems[comboTriggerMode.SelectedIndex].Checked = true;
			}
			m_menuReverseAxis.Checked = Settings.IsReverseAxis;
		}

		private void MenuDeviceSelect_Click(object sender, EventArgs e)
		{

		}

		private void MenuTriggerMode_Click(object sender, EventArgs e)
		{

		}

		private void MenuReverseAxis_Click(object sender, EventArgs e)
		{
			checkReverseAxis.IsChecked = !Settings.IsReverseAxis;
		}

		private void MenuExit_Click(object sender, EventArgs e)
		{
			m_notifyIcon.Visible = false;
			Close();
		}
	}
}
