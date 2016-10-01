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

			ProgramSettings.Load();
			IsLoading = true;
			triggerModeHappy.Value = ProgramSettings.TriggerHappyValue;
			triggerModeDouble.Value = ProgramSettings.TriggerDoubleValue;
			triggerModeTriple.Value = ProgramSettings.TriggerTripleValue;
			switch (ProgramSettings.TriggerMode) {
				case 0:
					triggerModeHappy.IsChecked = true;
					break;
				case 1:
					triggerModeDouble.IsChecked = true;
					break;
				case 2:
					triggerModeTriple.IsChecked = true;
					break;
			}
			checkHotKey.IsChecked = ProgramSettings.IsHotKey;
			checkReverseAxis.IsChecked = ProgramSettings.IsReverseAxis;
			check8Axis.IsChecked = ProgramSettings.Is8Axis;
			checkSnapAxis.IsChecked = ProgramSettings.IsSnapAxis;
			IsLoading = false;
			CreateNotifyIcon();
			StartServer();
			Native.KeyboardHook.KeyPressed += KeyboardHook_KeyPressed;
		}

		private void KeyboardHook_KeyPressed(object sender, Native.KeyboardHook.KeyEventArgs e)
		{
			if (!ProgramSettings.IsHotKey) {
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
			switch (ProgramSettings.TriggerMode) {
				case 0:
					m_timer = new ThreadTimer(Timer_Tick, RightTriggerAction.ActionInterval[0][ProgramSettings.TriggerHappyValue]);
					break;
				case 1:
					m_timer = new ThreadTimer(Timer_Tick, RightTriggerAction.ActionInterval[1][ProgramSettings.TriggerDoubleValue]);
					break;
				case 2:
					m_timer = new ThreadTimer(Timer_Tick, RightTriggerAction.ActionInterval[2][ProgramSettings.TriggerTripleValue]);
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
			if (!ProgramSettings.Save()) {
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

		void MenuTouchProfile_OnClick(object sender, RoutedEventArgs e)
		{
			new TouchProfileWindow(this).ShowDialog();
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
				ProgramSettings.TriggerMode = 0;
				ProgramSettings.TriggerHappyValue = triggerModeHappy.Value;
				m_timer.Change(RightTriggerAction.ActionInterval[0][ProgramSettings.TriggerHappyValue]);
			}
		}

		void TriggerModeDouble_OnSelected(object sender, EventArgs e)
		{
			if (!IsLoading) {
				triggerModeHappy.IsChecked = false;
				triggerModeTriple.IsChecked = false;
				ProgramSettings.TriggerMode = 1;
				ProgramSettings.TriggerDoubleValue = triggerModeDouble.Value;
				m_timer.Change(RightTriggerAction.ActionInterval[1][ProgramSettings.TriggerDoubleValue]);
			}
		}

		void TriggerModeTriple_OnSelected(object sender, EventArgs e)
		{
			if (!IsLoading) {
				triggerModeHappy.IsChecked = false;
				triggerModeDouble.IsChecked = false;
				ProgramSettings.TriggerMode = 2;
				ProgramSettings.TriggerTripleValue = triggerModeTriple.Value;
				m_timer.Change(RightTriggerAction.ActionInterval[2][ProgramSettings.TriggerTripleValue]);
			}
		}

		void CheckHotKey_OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (!IsLoading) {
				ProgramSettings.IsHotKey = checkHotKey.IsChecked == true;
			}
		}

		void checkReverseAxis_OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (!IsLoading) {
				ProgramSettings.IsReverseAxis = checkReverseAxis.IsChecked == true;
			}
		}

		void checkSnapAxis_OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (!IsLoading) {
				ProgramSettings.IsSnapAxis = checkSnapAxis.IsChecked == true;
			}
		}

		void check8Axis_OnCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (!IsLoading) {
				ProgramSettings.Is8Axis = check8Axis.IsChecked == true;
			}
		}
	}
}
