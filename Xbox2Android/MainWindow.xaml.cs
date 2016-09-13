using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Xbox2Android
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	partial class MainWindow : Window
	{
		readonly DispatcherTimer m_timer = new DispatcherTimer();

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
			m_timer.Tick += timer_Tick;
			m_timer.Interval = TimeSpan.FromSeconds(1.0 / 60.0);
			m_timer.Start();

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

		private void comboDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
		}

		private void comboTriggerMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ProgramSettings.TriggerMode = comboTriggerMode.SelectedIndex;
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
