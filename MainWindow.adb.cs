using System.IO;
using System.Windows;
using System.Windows.Controls;
using Managed.Adb;

namespace XboxInputMapper
{
	partial class MainWindow
	{
		AndroidDebugBridge m_adb;
		IDevice m_selectedDevice;

		void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
		{
			ReconnectAdb();
		}

		void ReconnectAdb()
		{
			ClearAdb();
			if (File.Exists(textAdbPath.Text)) {
				m_adb = AndroidDebugBridge.CreateBridge(textAdbPath.Text, false);
				m_adb.DeviceConnected += adb_DeviceConnected;
			}
		}

		void ClearAdb()
		{
			if (m_adb != null) {
				m_adb.DeviceConnected -= adb_DeviceConnected;
				m_adb = null;
			}
			comboDevices.Items.Clear();
		}

		private void adb_DeviceConnected(object sender, DeviceEventArgs e)
		{
			if (m_adb.IsConnected) {
				Dispatcher.Invoke(() => {
					comboDevices.Items.Add(new ComboBoxItem {
						Content = e.Device.SerialNumber,
						Tag = e.Device,
					});

					if (e.Device.SerialNumber == Settings.LastSelectedDevice) {
						comboDevices.SelectedIndex = comboDevices.Items.Count - 1;
					}
				});
			}
		}

		private void ComboDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (comboDevices.SelectedItem != null) {
				m_selectedDevice = (IDevice)((ComboBoxItem)comboDevices.SelectedItem).Tag;
				ResetGamepadState();
			}
		}

		void DoTouchDown(int index, Point point)
		{

		}

		void DoTouchUpdate(int index, Point point)
		{

		}

		void DoTouchUp(int index)
		{

		}

		void SendTouchData()
		{

		}
	}
}
