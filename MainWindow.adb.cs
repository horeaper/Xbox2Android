using System;
using System.Collections.Generic;
using System.Diagnostics;
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

		void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			ConnectAdb();
		}

		void ConnectAdb()
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
			ResetGamepadState();

			if (comboDevices.SelectedItem != null) {
				var command = new ShellCommandReceiver();
				command.Complete += GetDeviceCommandReceiver_Complete;
				m_selectedDevice = (IDevice)((ComboBoxItem)comboDevices.SelectedItem).Tag;
				m_selectedDevice.ExecuteShellCommand("getevent -pl", command);
			}
		}

		private void GetDeviceCommandReceiver_Complete(object sender, EventArgs e)
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

		const int EV_SYN = 0x0000;
		const int EV_KEY = 0x0001;
		const int EV_ABS = 0x0003;

		const int BTN_TOUCH = 0x014A;
		const int TOUCH_DOWN = 0x0001;
		const int TOUCH_UP = 0x0000;

		const int ABS_MT_PRESSURE = 0x003A;
		const int ABS_MT_POSITION_X = 0x0035;
		const int ABS_MT_POSITION_Y = 0x0036;
		const int SYN_MT_REPORT = 0x0002;
		const int SYN_REPORT = 0x0000;

		bool m_isPreviousTouchDown;
		Point? m_axisPosition;
		bool[] m_isButtonDown = new bool[Constants.ButtonCount];
		bool m_isDataDirty;
		List<Point> m_dataBuffer = new List<Point>(10);
		StringBuilder m_sendString = new StringBuilder();

		void AxisDown(Point point)
		{
			m_isDataDirty = true;
			m_axisPosition = point;
		}

		void AxisUpdate(Point point)
		{
			if (m_axisPosition.HasValue && m_axisPosition.Value == point) {
				return;
			}

			m_isDataDirty = true;
			m_axisPosition = point;
		}

		void AxisUp()
		{
			m_isDataDirty = true;
			m_axisPosition = null;
		}

		void ButtonDown(int index)
		{
			m_isDataDirty = true;
			m_isButtonDown[index] = true;
		}

		void ButtonUp(int index)
		{
			m_isDataDirty = true;
			m_isButtonDown[index] = false;
		}

		void SendTouchData()
		{
			if (m_isDataDirty && m_selectedDevice != null) {
				m_dataBuffer.Clear();
				if (m_axisPosition.HasValue) {
					m_dataBuffer.Add(m_axisPosition.Value);
				}
				for (int cnt = 0; cnt < Constants.ButtonCount; ++cnt) {
					if (m_isButtonDown[cnt]) {
						m_dataBuffer.AddRange(Settings.ButtonPositions[cnt]);
					}
				}

				m_sendString.Clear();
				Action<int, int, int> fnSendEvent = (eventType, inputType, param) => m_sendString.AppendFormat("sendevent {0} {1} {2} {3};", m_inputEventPath, eventType, inputType, param);

				if (m_dataBuffer.Count > 0) {
					if (!m_isPreviousTouchDown) {
						fnSendEvent(EV_KEY, BTN_TOUCH, TOUCH_DOWN);
						m_isPreviousTouchDown = true;
					}
					foreach (var point in m_dataBuffer) {
						int x = (int)Math.Round(point.X);
						int y = (int)Math.Round(point.Y);
						fnSendEvent(EV_ABS, ABS_MT_POSITION_X, x);
						fnSendEvent(EV_ABS, ABS_MT_POSITION_Y, y);
						fnSendEvent(EV_SYN, SYN_MT_REPORT, 0);
					}
					fnSendEvent(EV_SYN, SYN_REPORT, 0);
				}
				else {
					fnSendEvent(EV_SYN, SYN_MT_REPORT, 0);
					fnSendEvent(EV_SYN, SYN_REPORT, 0);
					if (m_isPreviousTouchDown) {
						fnSendEvent(EV_KEY, BTN_TOUCH, TOUCH_UP);
						fnSendEvent(EV_SYN, SYN_MT_REPORT, 0);
						fnSendEvent(EV_SYN, SYN_REPORT, 0);
						m_isPreviousTouchDown = false;
					}
				}

				m_selectedDevice.ExecuteShellCommand(m_sendString.ToString(), new ShellCommandReceiver());
				m_isDataDirty = false;
			}
		}
	}
}
