using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Xbox2Android.Native;

namespace Xbox2Android
{
	partial class MainWindow
	{
		const double ThumbDeadzone = short.MaxValue / 2.0;
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
					if (direction.Length <= ThumbDeadzone) {
						direction.X = 0;
						direction.Y = 0;
					}
					if (ProgramSettings.IsSnapAxis) {
						if (Math.Abs(direction.X) <= short.MaxValue / 3.0) {
							direction.X = 0;
						}
						if (Math.Abs(direction.Y) <= short.MaxValue / 3.0) {
							direction.Y = 0;
						}
					}
					if (direction.X == 0 && direction.Y == 0) {    //No direction
						if (m_isDirectionInEffect) {
							AxisUp();
							m_isDirectionInEffect = false;
						}
					}
					else {
						//8-axis
						if (ProgramSettings.Is8Axis) {
							var angle = Math.Abs(Math.Atan(direction.Y / direction.X) * (180 / Math.PI));
							if (angle > 0 && angle <= 22.5) {
								angle = 0;
							}
							else if (angle >= 22.5 && angle <= 67.5) {
								angle = 45;
							}
							else if (angle >= 45 + 22.5 && angle < 90) {
								angle = 90;
							}
							if (direction.X > 0) {
								if (angle == 0) {
									direction = new Vector(1, 0);
								}
								else if (angle == 45) {
									direction = direction.Y > 0 ? new Vector(1, 1) : new Vector(1, -1);
								}
								else if (angle == 90) {
									direction = direction.Y > 0 ? new Vector(0, 1) : new Vector(0, -1);
								}
							}
							else {
								if (angle == 0) {
									direction = new Vector(-1, 0);
								}
								else if (angle == 45) {
									direction = direction.Y > 0 ? new Vector(-1, 1) : new Vector(-1, -1);
								}
								else if (angle == 90) {
									direction = direction.Y > 0 ? new Vector(0, 1) : new Vector(0, -1);
								}
							}
						}

						//Normalize
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
				OnTimerTick();
			}
			else {
				ResetGamepadState();
			}
		}

		const short EV_SYN = 0x0000;
		const short EV_KEY = 0x0001;
		const short EV_ABS = 0x0003;

		const short BTN_TOUCH = 0x014A;
		const short TOUCH_DOWN = 0x0001;
		const short TOUCH_UP = 0x0000;

		const short ABS_MT_POSITION_X = 0x0035;
		const short ABS_MT_POSITION_Y = 0x0036;
		const short SYN_MT_REPORT = 0x0002;
		const short SYN_REPORT = 0x0000;

		bool m_isPreviousTouchDown;
		Point? m_axisPosition;
		bool[] m_isButtonDown = new bool[Constants.ButtonCount];
		bool m_isDataDirty;
		List<Point> m_dataBuffer = new List<Point>(10);
		BinaryWriter m_sendData = new BinaryWriter(new MemoryStream(1400));

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

		void OnTimerTick()
		{
			if (comboDevices.SelectedItem != null) {
				if (m_isDataDirty) {
					m_dataBuffer.Clear();
					if (m_axisPosition.HasValue) {
						m_dataBuffer.Add(m_axisPosition.Value);
					}
					for (int cnt = 0; cnt < Constants.ButtonCount; ++cnt) {
						if (m_isButtonDown[cnt]) {
							m_dataBuffer.AddRange(ProgramSettings.ButtonPositions[cnt]);
						}
					}

					m_sendData.BaseStream.Position = 0;
					Action<short, short, short> fnFormatString = (eventType, inputType, param) => {
						m_sendData.Write(eventType);
						m_sendData.Write(inputType);
						m_sendData.Write(param);
					};

					if (m_dataBuffer.Count > 0) {
						if (!m_isPreviousTouchDown) {
							fnFormatString(EV_KEY, BTN_TOUCH, TOUCH_DOWN);
							m_isPreviousTouchDown = true;
						}
						foreach (var point in m_dataBuffer) {
							short x = (short)Math.Round(point.X);
							short y = (short)Math.Round(point.Y);
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
					var data = new byte[m_sendData.BaseStream.Position];
					Array.Copy(((MemoryStream)m_sendData.BaseStream).GetBuffer(), data, data.Length);
					SendData((ClientParam)selectedItem.Tag, data);
					m_isDataDirty = false;
				}
			}
		}
	}
}
