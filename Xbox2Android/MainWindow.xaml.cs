using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
			Native.KeyboardHook.KeyPressed += KeyboardHook_KeyPressed;
		}

		private void KeyboardHook_KeyPressed(object sender, Native.KeyboardHook.KeyEventArgs e)
		{
			if (m_currentProfile == -1) {
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
			if (m_currentProfile == -1) {
				return;
			}

			switch (m_profiles[m_currentProfile].TriggerMode) {
				case 0:
					m_timer = new ThreadTimer(Timer_Tick, RightTriggerAction.ActionInterval[0][m_profiles[m_currentProfile].TriggerHappyValue]);
					break;
				case 1:
					m_timer = new ThreadTimer(Timer_Tick, RightTriggerAction.ActionInterval[1][m_profiles[m_currentProfile].TriggerDoubleValue]);
					break;
				case 2:
					m_timer = new ThreadTimer(Timer_Tick, RightTriggerAction.ActionInterval[2][m_profiles[m_currentProfile].TriggerTripleValue]);
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
		}

		void TriggerModeHappy_OnSelected(object sender, EventArgs e)
		{
			if (!IsLoading) {
				triggerModeDouble.IsChecked = false;
				triggerModeTriple.IsChecked = false;
				m_profiles[m_currentProfile].TriggerMode = 0;
				m_profiles[m_currentProfile].TriggerHappyValue = triggerModeHappy.Value;
				m_timer.Change(RightTriggerAction.ActionInterval[0][m_profiles[m_currentProfile].TriggerHappyValue]);
			}
		}

		void TriggerModeDouble_OnSelected(object sender, EventArgs e)
		{
			if (!IsLoading) {
				triggerModeHappy.IsChecked = false;
				triggerModeTriple.IsChecked = false;
				m_profiles[m_currentProfile].TriggerMode = 1;
				m_profiles[m_currentProfile].TriggerDoubleValue = triggerModeDouble.Value;
				m_timer.Change(RightTriggerAction.ActionInterval[1][m_profiles[m_currentProfile].TriggerDoubleValue]);
			}
		}

		void TriggerModeTriple_OnSelected(object sender, EventArgs e)
		{
			if (!IsLoading) {
				triggerModeHappy.IsChecked = false;
				triggerModeDouble.IsChecked = false;
				m_profiles[m_currentProfile].TriggerMode = 2;
				m_profiles[m_currentProfile].TriggerTripleValue = triggerModeTriple.Value;
				m_timer.Change(RightTriggerAction.ActionInterval[2][m_profiles[m_currentProfile].TriggerTripleValue]);
			}
		}

		void checkReverseAxis_OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (!IsLoading) {
				m_profiles[m_currentProfile].IsReverseAxis = checkReverseAxis.IsChecked == true;
			}
		}

		void checkSnapAxis_OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (!IsLoading) {
				m_profiles[m_currentProfile].IsSnapAxis = checkSnapAxis.IsChecked == true;
			}
		}

		void check8Axis_OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (!IsLoading) {
				m_profiles[m_currentProfile].Is8Axis = check8Axis.IsChecked == true;
			}
		}
	}
}
