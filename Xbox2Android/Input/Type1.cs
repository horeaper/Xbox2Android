using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Xbox2Android.Input
{
	class Type1 : IInput
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

		bool m_isPreviousTouchDown;
		Point? m_axisPosition;
		bool[] m_isButtonDown = new bool[Constants.ButtonCount];
		bool m_isDataDirty;
		readonly List<Point> m_dataBuffer = new List<Point>(10);

		readonly BinaryWriter m_sendData = new BinaryWriter(new MemoryStream(1400));

		short PositionX(Point point, Property prop)
		{
			return (short)Math.Round(point.X / prop.Profile.Width * prop.Client.Width);
		}

		short PositionY(Point point, Property prop)
		{
			return (short)Math.Round(point.Y / prop.Profile.Height * prop.Client.Height);
		}

		void FormatEvent(short eventType, short inputType, short param = 0)
		{
			m_sendData.Write(eventType);
			m_sendData.Write(inputType);
			m_sendData.Write(param);
		}

		//========================================================================
		// Events
		//========================================================================

		public void AxisDown(Point point, Property prop)
		{
			if (!m_axisPosition.HasValue) {
				m_axisPosition = point;
				m_isDataDirty = true;
			}
		}

		public void AxisUpdate(Point point, Property prop)
		{
			if (m_axisPosition == null || m_axisPosition.Value != point) {
				m_axisPosition = point;
				m_isDataDirty = true;
			}
		}

		public void AxisUp(Property prop)
		{
			if (m_axisPosition != null) {
				m_axisPosition = null;
				m_isDataDirty = true;
			}
		}

		public void ButtonDown(int index, Property prop)
		{
			if (!m_isButtonDown[index]) {
				m_isButtonDown[index] = true;
				m_isDataDirty = true;
			}
		}

		public void ButtonUp(int index, Property prop)
		{
			if (m_isButtonDown[index]) {
				m_isButtonDown[index] = false;
				m_isDataDirty = true;
			}
		}

		public void FrameUpdate(Property prop)
		{
			if (m_isDataDirty) {
				m_dataBuffer.Clear();
				if (m_axisPosition.HasValue) {
					m_dataBuffer.Add(m_axisPosition.Value);
				}
				for (int cnt = 0; cnt < Constants.ButtonCount; ++cnt) {
					if (m_isButtonDown[cnt]) {
						m_dataBuffer.AddRange(prop.Profile.ButtonPositions[cnt]);
					}
				}

				m_sendData.BaseStream.Position = 0;
				if (m_dataBuffer.Count > 0) {
					if (!m_isPreviousTouchDown) {
						FormatEvent(EV_KEY, BTN_TOUCH, TOUCH_DOWN);
						m_isPreviousTouchDown = true;
					}
					foreach (var point in m_dataBuffer) {
						FormatEvent(EV_ABS, ABS_MT_POSITION_X, PositionX(point, prop));
						FormatEvent(EV_ABS, ABS_MT_POSITION_Y, PositionY(point, prop));
						FormatEvent(EV_SYN, SYN_MT_REPORT);
					}
					FormatEvent(EV_SYN, SYN_REPORT);
				}
				else {
					FormatEvent(EV_SYN, SYN_MT_REPORT);
					FormatEvent(EV_SYN, SYN_REPORT);
					if (m_isPreviousTouchDown) {
						FormatEvent(EV_KEY, BTN_TOUCH, TOUCH_UP);
						FormatEvent(EV_SYN, SYN_MT_REPORT);
						FormatEvent(EV_SYN, SYN_REPORT);
						m_isPreviousTouchDown = false;
					}
				}

				var data = new byte[m_sendData.BaseStream.Position];
				Array.Copy(((MemoryStream)m_sendData.BaseStream).GetBuffer(), data, data.Length);
				prop.SendData(data);
				m_isDataDirty = false;
			}
		}
	}
}
