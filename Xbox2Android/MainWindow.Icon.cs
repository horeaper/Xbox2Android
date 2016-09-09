using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;

namespace Xbox2Android
{
	partial class MainWindow
	{
		NotifyIcon m_notifyIcon;
		ContextMenu m_iconMenu;

		MenuItem m_menuReverseAxis;
		MenuItem m_menuExit;

		void CreateNotifyIcon()
		{
			m_menuReverseAxis = new MenuItem("Reverse Axis");
			m_menuReverseAxis.Click += MenuReverseAxis_Click;
			m_menuExit = new MenuItem("Exit");
			m_menuExit.Click += MenuExit_Click;

			m_iconMenu = new ContextMenu();
			m_iconMenu.Popup += ContextMenu_Popup;

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
			m_iconMenu.MenuItems.Clear();

			m_iconMenu.MenuItems.Add(new MenuItem("Connected") { Enabled = false });
			foreach (ComboBoxItem item in comboDevices.Items) {
				var menuItem = new MenuItem(item.Content.ToString());
				menuItem.Click += MenuDeviceSelect_Click;
				menuItem.Tag = item;
				m_iconMenu.MenuItems.Add(menuItem);
				if (ReferenceEquals(item, comboDevices.SelectedItem)) {
					menuItem.Checked = true;
				}
			}

			m_iconMenu.MenuItems.Add(new MenuItem("-"));

/*
			m_iconMenu.MenuItems.Add(new MenuItem("Trigger Mode") { Enabled = false });
			foreach (ComboBoxItem item in comboTriggerMode.Items) {
				var menuItem = new MenuItem(item.Content.ToString());
				menuItem.Click += MenuTriggerMode_Click;
				menuItem.Tag = item;
				m_iconMenu.MenuItems.Add(menuItem);
				if (ReferenceEquals(item, comboTriggerMode.SelectedItem)) {
					menuItem.Checked = true;
				}
			}

			m_iconMenu.MenuItems.Add(new MenuItem("-"));
*/

			m_menuReverseAxis.Checked = ProgramSettings.IsReverseAxis;
			m_iconMenu.MenuItems.AddRange(new[] { m_menuReverseAxis, new MenuItem("-"), m_menuExit });
		}

		private void MenuDeviceSelect_Click(object sender, EventArgs e)
		{
			comboDevices.SelectedItem = ((MenuItem)sender).Tag;
		}

		private void MenuTriggerMode_Click(object sender, EventArgs e)
		{
			comboTriggerMode.SelectedItem = ((MenuItem)sender).Tag;
		}

		private void MenuReverseAxis_Click(object sender, EventArgs e)
		{
			checkReverseAxis.IsChecked = !ProgramSettings.IsReverseAxis;
		}

		private void MenuExit_Click(object sender, EventArgs e)
		{
			m_notifyIcon.Visible = false;
			Close();
		}
	}
}
