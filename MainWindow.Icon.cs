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
		MenuItem m_menuTriggerHappy;
		MenuItem m_menuReverseAxis;
		MenuItem m_menuExit;

		void InitializeNotifyIcon()
		{
			m_menuTriggerHappy = new MenuItem("Trigger Happy");
			m_menuTriggerHappy.Click += MenuTriggerHappy_Click;
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

		private void ContextMenu_Popup(object sender, EventArgs e)
		{
			m_iconMenu.MenuItems.Clear();

			if (comboDevices.Items.Count > 0) {
				foreach (System.Windows.Controls.ComboBoxItem item in comboDevices.Items) {
					var deviceItem = new MenuItem(item.Content.ToString());
					if (ReferenceEquals(item, comboDevices.SelectedItem)) {
						deviceItem.Checked = true;
					}
					m_iconMenu.MenuItems.Add(deviceItem);
				}
				m_iconMenu.MenuItems.Add(new MenuItem("-"));
			}
			m_iconMenu.MenuItems.AddRange(new[] { m_menuTriggerHappy, m_menuReverseAxis, new MenuItem("-"), m_menuExit });

			m_menuTriggerHappy.Checked = Settings.IsTriggerHappy;
			m_menuReverseAxis.Checked = Settings.IsReverseAxis;
		}

		private void MenuTriggerHappy_Click(object sender, EventArgs e)
		{
			checkTriggerHappy.IsChecked = !Settings.IsTriggerHappy;
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

		private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left) {
				Show();
				WindowState = WindowState.Normal;
				m_notifyIcon.Visible = false;
			}
		}
	}
}
