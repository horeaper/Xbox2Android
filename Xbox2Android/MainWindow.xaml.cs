using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace Xbox2Android
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	sealed partial class MainWindow : Window
	{
		[DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		static extern bool QueryPerformanceFrequency(out long frequency);
		[DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		static extern bool QueryPerformanceCounter(out long counter);

		ThreadTimer m_timer;
		ClientParam m_selectedClient;

		public MainWindow()
		{
			InitializeComponent();

			ProgramSettings.Load();
			comboTriggerMode.SelectedIndex = ProgramSettings.TriggerMode;
			checkReverseAxis.IsChecked = ProgramSettings.IsReverseAxis;
			check8Axis.IsChecked = ProgramSettings.Is8Axis;
			checkSnapAxis.IsChecked = ProgramSettings.IsSnapAxis;
			CreateNotifyIcon();
			StartServer();
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			m_timer = new ThreadTimer(Timer_Tick, 1 / 30.0f);
			WindowState = ProgramSettings.IsMinimized ? WindowState.Minimized : WindowState.Normal;
			MainWindow_StateChanged(null, null);
		}

		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			ProgramSettings.IsMinimized = WindowState == WindowState.Minimized;
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

		private void MainWindow_StateChanged(object sender, EventArgs e)
		{
			if (WindowState == WindowState.Minimized) {
				Hide();
				m_notifyIcon.Visible = true;
			}
		}

		private void comboClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			m_selectedClient = (ClientParam)((ComboBoxItem)comboClients.SelectedItem)?.Tag;
		}

		private void comboTriggerMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ProgramSettings.TriggerMode = comboTriggerMode.SelectedIndex;
			m_timer?.Change(RightTriggerAction.ActionInterval[ProgramSettings.TriggerMode]);
		}

		private void checkReverseAxis_CheckedChanged(object sender, RoutedEventArgs e)
		{
			ProgramSettings.IsReverseAxis = checkReverseAxis.IsChecked == true;
		}

		private void checkSnapAxis_CheckedChanged(object sender, RoutedEventArgs e)
		{
			ProgramSettings.IsSnapAxis = checkSnapAxis.IsChecked == true;
		}

		private void check8Axis_CheckedChanged(object sender, RoutedEventArgs e)
		{
			ProgramSettings.Is8Axis = check8Axis.IsChecked == true;
		}

		private void btnTouchProfile_Click(object sender, RoutedEventArgs e)
		{
			var profileWindow = new TouchProfileWindow();
			profileWindow.Owner = this;
			profileWindow.ShowDialog();
		}
	}
}
