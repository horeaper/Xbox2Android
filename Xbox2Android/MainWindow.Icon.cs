using System;
using System.Windows;
using System.Windows.Forms;

namespace Xbox2Android
{
	partial class MainWindow
	{
		NotifyIcon m_notifyIcon;

		ContextMenu m_iconMenu;
		MenuItem m_menuExit;

		void CreateNotifyIcon()
		{
			m_menuExit = new MenuItem("Exit");
			m_menuExit.Click += MenuExit_Click;

			m_iconMenu = new ContextMenu();
			m_iconMenu.MenuItems.Add(m_menuExit);

			m_notifyIcon = new NotifyIcon();
			m_notifyIcon.Icon = Properties.Resources.Program;
			m_notifyIcon.Visible = true;
			m_notifyIcon.Text = @"Xbox 2 Android";
			m_notifyIcon.ContextMenu = m_iconMenu;
			m_notifyIcon.MouseClick += NotifyIcon_MouseClick;
		}

		private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left) {
				if (IsVisible) {
					Hide();
				}
				else {
					Show();
				}
			}
		}

		private void MenuExit_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
