﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Xbox2Android.Native;

namespace Xbox2Android
{
	static class InputMapper
	{
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

		static bool m_isPreviousTouchDown;
		static Point? m_axisPosition;
		static bool[] m_isButtonDown = new bool[Constants.ButtonCount];
		static bool m_isDataDirty;
		static readonly List<Point> m_dataBuffer = new List<Point>(10);
		static readonly BinaryWriter m_sendData = new BinaryWriter(new MemoryStream(1400));

		public static void AxisDown(Point point)
		{
			m_isDataDirty = true;
			m_axisPosition = point;
		}

		public static void AxisUpdate(Point point)
		{
			if (m_axisPosition.HasValue && m_axisPosition.Value == point) {
				return;
			}
			m_isDataDirty = true;
			m_axisPosition = point;
		}

		public static void AxisUp()
		{
			m_isDataDirty = true;
			m_axisPosition = null;
		}

		public static void ButtonDown(XInput.GamePadButton button)
		{
			ButtonDown(Array.IndexOf(Constants.ButtonValue, button));
		}

		public static void ButtonDown(int index)
		{
			m_isDataDirty = true;
			m_isButtonDown[index] = true;
		}

		public static void ButtonUp(XInput.GamePadButton button)
		{
			ButtonUp(Array.IndexOf(Constants.ButtonValue, button));
		}

		public static void ButtonUp(int index)
		{
			m_isDataDirty = true;
			m_isButtonDown[index] = false;
		}

		public static void FrameUpdate(MainWindow.ClientParam selectedClient, Action<MainWindow.ClientParam, byte[]> fnSendData)
		{
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

				var data = new byte[m_sendData.BaseStream.Position];
				Array.Copy(((MemoryStream)m_sendData.BaseStream).GetBuffer(), data, data.Length);
				fnSendData(selectedClient, data);
				m_isDataDirty = false;
			}
		}
	}
}
