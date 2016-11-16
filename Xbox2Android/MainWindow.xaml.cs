using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xbox2Android.Input;

namespace Xbox2Android
{
	sealed partial class MainWindow : Window
	{
		public static bool IsLoading;
		ThreadTimer m_timer;
		ClientParam m_selectedClient;

		public MainWindow()
		{
			InitializeComponent();

			LoadSettings();
			CreateNotifyIcon();
			StartServer();
			InputMapper.SendDataCallback = SendData;
			Native.KeyboardHook.KeyPressed += KeyboardHook_KeyPressed;
		}

		private void KeyboardHook_KeyPressed(object sender, Native.KeyboardHook.KeyEventArgs e)
		{
			if (m_selectedProfileIndex == -1) {
				return;
			}
			if (!CurrentProfile.IsHotKey) {
				return;
			}

			Dispatcher.InvokeAsync(() => {
				switch (e.Key) {
					case Key.NumPad7:
					case Key.NumPad8:
						triggerModeHappy.Value = 0;
						triggerModeHappy.IsChecked = true;
						break;
					case Key.NumPad9:
						triggerModeHappy.Value = 1;
						triggerModeHappy.IsChecked = true;
						break;
					case Key.NumPad4:
						triggerModeDouble.Value = 0;
						triggerModeDouble.IsChecked = true;
						break;
					case Key.NumPad5:
						triggerModeDouble.Value = 1;
						triggerModeDouble.IsChecked = true;
						break;
					case Key.NumPad6:
						triggerModeDouble.Value = 2;
						triggerModeDouble.IsChecked = true;
						break;
					case Key.NumPad1:
						triggerModeTriple.Value = 0;
						triggerModeTriple.IsChecked = true;
						break;
					case Key.NumPad2:
						triggerModeTriple.Value = 1;
						triggerModeTriple.IsChecked = true;
						break;
					case Key.NumPad3:
						triggerModeTriple.Value = 2;
						triggerModeTriple.IsChecked = true;
						break;
					case Key.NumPad0:
						{
							if (listClients.Items.Count > 0) {
								int index = listClients.SelectedIndex + 1;
								index %= listClients.Items.Count;
								listClients.SelectedIndex = index;
							}
						}
						break;
				}
			});
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			if (m_selectedProfileIndex == -1) {
				return;
			}

			switch (CurrentProfile.TriggerMode) {
				case 0:
					m_timer = new ThreadTimer(Timer_Tick, RightTriggerAction.ActionInterval[0][CurrentProfile.TriggerHappyValue]);
					break;
				case 1:
					m_timer = new ThreadTimer(Timer_Tick, RightTriggerAction.ActionInterval[1][CurrentProfile.TriggerDoubleValue]);
					break;
				case 2:
					m_timer = new ThreadTimer(Timer_Tick, RightTriggerAction.ActionInterval[2][CurrentProfile.TriggerTripleValue]);
					break;
			}

			if (WindowState == WindowState.Normal) {
				var workArea = SystemParameters.WorkArea;
				this.Left = workArea.Left + workArea.Width - this.Width;
				this.Top = workArea.Top;
			}
		}

		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			m_notifyIcon.Visible = false;

tagRetry:
			if (!SaveSettings()) {
				var result = MessageBox.Show(this, "Cannot save program settings. Retry?", "Xbox Input Mapper", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
				if (result == MessageBoxResult.Yes) {
					goto tagRetry;
				}
			}

			m_timer.Stop();
			Environment.Exit(0);
		}

		void LayoutRoot_OnMouseEnter(object sender, MouseEventArgs e)
		{
			this.Opacity = 1;
		}

		void LayoutRoot_OnMouseLeave(object sender, MouseEventArgs e)
		{
			this.Opacity = 0.5;
		}

		void MenuExit_OnClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		void listClients_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			m_selectedClient = (ClientParam)((ListBoxItem)listClients.SelectedItem)?.Tag;
			InputMapper.Client = m_selectedClient;

			if (m_selectedClient != null) {
				for (int index = 0; index < m_profiles.Count; index++) {
					var profile = m_profiles[index];
					if (profile.Name == m_selectedClient.Name || m_selectedClient.Name.StartsWith(profile.Name + "-")) {
						SelectProfileByIndex(index);
						break;
					}
				}
			}
		}

		void TriggerModeHappy_OnSelected(object sender, EventArgs e)
		{
			if (!IsLoading) {
				triggerModeDouble.IsChecked = false;
				triggerModeTriple.IsChecked = false;
				CurrentProfile.TriggerMode = 0;
				CurrentProfile.TriggerHappyValue = triggerModeHappy.Value;
				m_timer.Change(RightTriggerAction.ActionInterval[0][CurrentProfile.TriggerHappyValue]);
			}
		}

		void TriggerModeDouble_OnSelected(object sender, EventArgs e)
		{
			if (!IsLoading) {
				triggerModeHappy.IsChecked = false;
				triggerModeTriple.IsChecked = false;
				CurrentProfile.TriggerMode = 1;
				CurrentProfile.TriggerDoubleValue = triggerModeDouble.Value;
				m_timer.Change(RightTriggerAction.ActionInterval[1][CurrentProfile.TriggerDoubleValue]);
			}
		}

		void TriggerModeTriple_OnSelected(object sender, EventArgs e)
		{
			if (!IsLoading) {
				triggerModeHappy.IsChecked = false;
				triggerModeDouble.IsChecked = false;
				CurrentProfile.TriggerMode = 2;
				CurrentProfile.TriggerTripleValue = triggerModeTriple.Value;
				m_timer.Change(RightTriggerAction.ActionInterval[2][CurrentProfile.TriggerTripleValue]);
			}
		}

		void CheckHotKey_OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (!IsLoading) {
				CurrentProfile.IsHotKey = checkHotKey.IsChecked == true;
			}
		}

		void checkReverseAxis_OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (!IsLoading) {
				CurrentProfile.IsReverseAxis = checkReverseAxis.IsChecked == true;
			}
		}

		void checkSnapAxis_OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (!IsLoading) {
				CurrentProfile.IsSnapAxis = checkSnapAxis.IsChecked == true;
			}
		}

		void check8Axis_OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (!IsLoading) {
				CurrentProfile.Is8Axis = check8Axis.IsChecked == true;
			}
		}
	}
}
