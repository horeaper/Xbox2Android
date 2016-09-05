using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Xbox2Android.Native;

namespace Xbox2Android
{
	partial class MainWindow
	{
		const int ThumbDeadzone = short.MaxValue / 4;
		const int TriggerDeadzone = byte.MaxValue / 2;
		XInput.Gamepad m_previousGamepad;
		bool m_isDirectionInEffect;
		bool m_isShadowAxis;

		void ResetGamepadState()
		{
			m_previousGamepad = new XInput.Gamepad();
			m_isDirectionInEffect = false;
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			XInput.State state;
			if (XInput.GetState(0, out state) == XInput.ErrorSuccess) {
				//Axis
				if (ProgramSettings.AxisCenter.HasValue && ProgramSettings.AxisRadius > 0) {
					var direction = new Vector(state.Gamepad.ThumbLX, state.Gamepad.ThumbLY);
					if (Math.Abs(direction.X) <= ThumbDeadzone) {
						direction.X = 0;
					}
					if (Math.Abs(direction.Y) <= ThumbDeadzone) {
						direction.Y = 0;
					}
					if (direction.X == 0 && direction.Y == 0) {    //No direction
						if (m_isDirectionInEffect) {
							AxisUp();
							m_isDirectionInEffect = false;
						}
					}
					else {
						direction.Normalize();
						direction *= ProgramSettings.AxisRadius;

						//Reverse axis
						if (ProgramSettings.IsReverseAxis) {
							direction.X = -direction.X;
							direction.Y = -direction.Y;
						}

						//Shadow axis
						if (direction.X > 0) {
							m_isShadowAxis = false;
						}
						else if (direction.X < 0) {
							m_isShadowAxis = true;
						}
						var axisCenter = ProgramSettings.AxisCenter.Value;
						if (m_isShadowAxis) {
							axisCenter.X += ProgramSettings.ShadowAxisOffset;
						}

						//Output
						var point = new Point(axisCenter.X + direction.X, axisCenter.Y - direction.Y);
						if (!m_isDirectionInEffect) {
							AxisDown(point);
							m_isDirectionInEffect = true;
						}
						else {
							AxisUpdate(point);
						}
					}
				}

				var gamepad = state.Gamepad;

				//Button
				for (int buttonId = 0; buttonId < Constants.ButtonValue.Length; ++buttonId) {
					var flag = Constants.ButtonValue[buttonId];
					bool isButtonInEffect = m_previousGamepad.Buttons.HasFlag(flag);
					bool isButtonDown = gamepad.Buttons.HasFlag(flag);
					if (isButtonDown) {
						if (!isButtonInEffect) {
							ButtonDown(buttonId);
						}
					}
					else {
						if (isButtonInEffect) {
							ButtonUp(buttonId);
						}
					}
				}

				m_previousGamepad = gamepad;
				SendTouchData();
			}
			else {
				ResetGamepadState();
			}
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
			if (m_isDataDirty && comboDevices.SelectedItem != null) {
				m_dataBuffer.Clear();
				if (m_axisPosition.HasValue) {
					m_dataBuffer.Add(m_axisPosition.Value);
				}
				for (int cnt = 0; cnt < Constants.ButtonCount; ++cnt) {
					if (m_isButtonDown[cnt]) {
						m_dataBuffer.AddRange(ProgramSettings.ButtonPositions[cnt]);
					}
				}

				m_sendString.Clear();
				Action<int, int, int> fnFormatString = (eventType, inputType, param) => m_sendString.AppendFormat("{0} {1} {2}\n", eventType, inputType, param);

				if (m_dataBuffer.Count > 0) {
					if (!m_isPreviousTouchDown) {
						fnFormatString(EV_KEY, BTN_TOUCH, TOUCH_DOWN);
						m_isPreviousTouchDown = true;
					}
					foreach (var point in m_dataBuffer) {
						int x = (int)Math.Round(point.X);
						int y = (int)Math.Round(point.Y);
						fnFormatString(EV_ABS, ABS_MT_POSITION_X, x);
						fnFormatString(EV_ABS, ABS_MT_POSITION_Y, y);
						fnFormatString(EV_SYN, SYN_MT_REPORT, 0);
					}
					fnFormatString(EV_SYN, SYN_REPORT, 0);
				}
				else {
					fnFormatString(EV_SYN, SYN_MT_REPORT, 0);
					fnFormatString(EV_SYN, SYN_REPORT, 0);
					if (m_isPreviousTouchDown) {
						fnFormatString(EV_KEY, BTN_TOUCH, TOUCH_UP);
						fnFormatString(EV_SYN, SYN_MT_REPORT, 0);
						fnFormatString(EV_SYN, SYN_REPORT, 0);
						m_isPreviousTouchDown = false;
					}
				}

				var selectedItem = (ComboBoxItem)comboDevices.SelectedItem;
				SendString((ClientParam)selectedItem.Tag, m_sendString.ToString());
				m_isDataDirty = false;
			}
		}
	}
}
