using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xbox2Android.Native;

namespace Xbox2Android.Input
{
	class Type2 : IInput
	{
		const short EV_SYN = 0x0000;
		const short EV_KEY = 0x0001;
		const short EV_ABS = 0x0003;

		const short BTN_TOUCH = 0x014A;
		const short TOUCH_DOWN = 0x0001;
		const short TOUCH_UP = 0x0000;

		const short ABS_X = 0x0000;
		const short ABS_Y = 0x0001;
		const short ABS_MT_SLOT = 0x002F;
		const short ABS_MT_POSITION_X = 0x0035;
		const short ABS_MT_POSITION_Y = 0x0036;
		const short ABS_MT_TRACKING_ID = 0x0039;
		const short SYN_REPORT = 0x0000;

		short m_currentTrackingId = 1;
		readonly bool[] m_slotStatus = new bool[16];

		bool m_isPreviousTouchDown;
		short? m_axisSlot;
		readonly List<short>[] m_buttonSlot = new List<short>[Constants.ButtonCount];

		readonly BinaryWriter m_sendData = new BinaryWriter(new MemoryStream(1400));

		public Type2()
		{
			for (int cnt = 0; cnt < m_buttonSlot.Length; ++cnt) {
				m_buttonSlot[cnt] = new List<short>();
			}
		}

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

		void SendBuffer(Property prop)
		{
			var data = new byte[m_sendData.BaseStream.Position];
			Array.Copy(((MemoryStream)m_sendData.BaseStream).GetBuffer(), data, data.Length);
			prop.SendData(data);
			m_sendData.BaseStream.Position = 0;
		}

		void OnTouchDown()
		{
			if (!m_isPreviousTouchDown) {
				FormatEvent(EV_KEY, BTN_TOUCH, TOUCH_DOWN);
				m_isPreviousTouchDown = true;
			}
		}

		void OnTouchUp()
		{
			bool isTouchDown = m_axisSlot.HasValue || m_buttonSlot.Any(item => item.Count > 0);
			if (!isTouchDown) {
				FormatEvent(EV_KEY, BTN_TOUCH, TOUCH_UP);
				m_isPreviousTouchDown = false;
			}
		}

		void OnTouchPointUpdate(short slot, Point point, Property prop)
		{
			if (slot == 0) {
				FormatEvent(EV_ABS, ABS_X, PositionX(point, prop));
				FormatEvent(EV_ABS, ABS_Y, PositionY(point, prop));
			}
		}

		short GetSlot()
		{
			for (short cnt = 0; cnt < m_slotStatus.Length; ++cnt) {
				if (!m_slotStatus[cnt]) {
					m_slotStatus[cnt] = true;
					return cnt;
				}
			}
			return -1;
		}

		void PutSlot(short index)
		{
			m_slotStatus[index] = false;
		}

		short GetTrackingId()
		{
			return m_currentTrackingId++;
		}

		//========================================================================
		// Events
		//========================================================================
		
		public void AxisDown(Point point, Property prop)
		{
			Debug.Assert(!m_axisSlot.HasValue);
			m_axisSlot = GetSlot();

			OnTouchDown();
			FormatEvent(EV_ABS, ABS_MT_SLOT, m_axisSlot.Value);
			FormatEvent(EV_ABS, ABS_MT_TRACKING_ID, GetTrackingId());
			FormatEvent(EV_ABS, ABS_MT_POSITION_X, PositionX(point, prop));
			FormatEvent(EV_ABS, ABS_MT_POSITION_Y, PositionY(point, prop));
			OnTouchPointUpdate(m_axisSlot.Value, point, prop);

			FormatEvent(EV_SYN, SYN_REPORT);
			SendBuffer(prop);
		}

		public void AxisUpdate(Point point, Property prop)
		{
			Debug.Assert(m_axisSlot.HasValue);

			FormatEvent(EV_ABS, ABS_MT_SLOT, m_axisSlot.Value);
			FormatEvent(EV_ABS, ABS_MT_POSITION_X, PositionX(point, prop));
			FormatEvent(EV_ABS, ABS_MT_POSITION_Y, PositionY(point, prop));
			OnTouchPointUpdate(m_axisSlot.Value, point, prop);

			FormatEvent(EV_SYN, SYN_REPORT);
			SendBuffer(prop);
		}

		public void AxisUp(Property prop)
		{
			Debug.Assert(m_axisSlot.HasValue);

			FormatEvent(EV_ABS, ABS_MT_SLOT, m_axisSlot.Value);
			FormatEvent(EV_ABS, ABS_MT_TRACKING_ID, -1);

			PutSlot(m_axisSlot.Value);
			m_axisSlot = null;
			OnTouchUp();

			FormatEvent(EV_SYN, SYN_REPORT);
			SendBuffer(prop);
		}

		public void ButtonDown(int index, Property prop)
		{
			if (m_buttonSlot[index].Count == 0) {
				var slots = m_buttonSlot[index];

				var points = prop.Profile.ButtonPositions[index];
				for (int cnt = 0; cnt < points.Count; ++cnt) {
					slots.Add(GetSlot());
				}

				OnTouchDown();
				for (int cnt = 0; cnt < slots.Count; ++cnt) {
					FormatEvent(EV_ABS, ABS_MT_SLOT, slots[cnt]);
					FormatEvent(EV_ABS, ABS_MT_TRACKING_ID, GetTrackingId());
					FormatEvent(EV_ABS, ABS_MT_POSITION_X, PositionX(points[cnt], prop));
					FormatEvent(EV_ABS, ABS_MT_POSITION_Y, PositionY(points[cnt], prop));
					OnTouchPointUpdate(slots[cnt], points[cnt], prop);
				}

				FormatEvent(EV_SYN, SYN_REPORT);
				SendBuffer(prop);
			}
		}

		public void ButtonUp(int index, Property prop)
		{
			if (m_buttonSlot[index].Count > 0) {
				var slots = m_buttonSlot[index];

				foreach (short slot in slots) {
					FormatEvent(EV_ABS, ABS_MT_SLOT, slot);
					FormatEvent(EV_ABS, ABS_MT_TRACKING_ID, -1);
				}

				foreach (var slot in slots) {
					PutSlot(slot);
				}
				slots.Clear();
				OnTouchUp();

				FormatEvent(EV_SYN, SYN_REPORT);
				SendBuffer(prop);
			}
		}

		public void FrameUpdate(Property prop)
		{
		}
	}
}
