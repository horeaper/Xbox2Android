using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Managed.Adb;

namespace XboxInputMapper
{
	partial class MainWindow
	{
		AndroidDebugBridge m_adb;
		IDevice m_selectedDevice;
		volatile string m_inputEventPath;

		class ShellCommandReceiver : IShellOutputReceiver
		{
			public string Content { get; private set; }
			public bool IsCancelled => false;
			StringBuilder m_builder = new StringBuilder();

			public event EventHandler Complete;

			public void AddOutput(byte[] data, int offset, int length)
			{
				for (int cnt = 0; cnt < length; ++cnt) {
					m_builder.Append((char)data[offset + cnt]);
				}
			}

			public void Flush()
			{
				var result = new StringBuilder(m_builder.Length);
				foreach (var c in m_builder.ToString()) {
					if (c != '\r') {
						result.Append(c);
					}
				}
				Content = result.ToString();
				Complete?.Invoke(this, EventArgs.Empty);
			}
		}

		void btnRefresh_Click(object sender, RoutedEventArgs e)
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
			m_selectedDevice = null;
			m_inputEventPath = null;
			Dispatcher.Invoke(() => textInputEvent.Text = "");
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

		private void comboDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			m_selectedDevice = null;
			m_inputEventPath = null;
			Dispatcher.Invoke(() => textInputEvent.Text = "");

			if (comboDevices.SelectedItem != null) {
				m_selectedDevice = (IDevice)((ComboBoxItem)comboDevices.SelectedItem).Tag;
				ResetGamepadState();

				var command = new ShellCommandReceiver();
				command.Complete += CommandReceiver_Complete;
				m_selectedDevice.ExecuteShellCommand("getevent -pl", command);
			}
		}

		private void CommandReceiver_Complete(object sender, EventArgs e)
		{
			var command = (ShellCommandReceiver)sender;

			var deviceCollection = new List<string>();
			var builder = new StringBuilder();
			foreach (var line in command.Content.Split('\n')) {
				if (line.Length > 0) {
					if (line[0] != ' ') {
						if (builder.Length > 0) {
							deviceCollection.Add(builder.ToString());
							builder.Clear();
						}
					}

					builder.Append(line);
					builder.Append('\n');
				}
			}

			string inputDeviceDescription = deviceCollection.FirstOrDefault(text => text.Contains("ABS_MT_POSITION_X") && text.Contains("ABS_MT_POSITION_Y"));
			if (inputDeviceDescription == null) {
				return;
			}
			var lines = inputDeviceDescription.Split('\n');
			int colonIndex = lines[0].IndexOf(':');
			if (colonIndex == -1) {
				return;
			}
			m_inputEventPath = lines[0].Substring(colonIndex + 1).Trim();
			Dispatcher.Invoke(() => textInputEvent.Text = m_inputEventPath);
		}

		const int EV_ABS = 3;
		const int EV_SYN = 0;

		const int BTN_TOUCH = 0x014A;
		const int TOUCH_DOWN = 1;
		const int TOUCH_UP = 0;

		const int ABS_MT_POSITION_X = 53;
		const int ABS_MT_POSITION_Y = 54;
		const int ABS_MT_PRESSURE = 58;
		const int SYN_MT_REPORT = 2;
		const int SYN_REPORT = 0;

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
