using System;
using System.Diagnostics;
using System.Windows;
using Xbox2Android.Native;

namespace Xbox2Android.Input
{
	static class InputMapper
	{
		static readonly IInput[] m_input = new IInput[2] {
			new Type1(),
			new Type2(),
		};
		static readonly Property m_prop = new Property();

		public static MainWindow.ClientParam Client
		{
			get { return m_prop.Client; }
			set { m_prop.Client = value; }
		}

		public static TouchProfile Profile
		{
			get { return m_prop.Profile; }
			set { m_prop.Profile = value; }

		}
		public static Action<byte[]> SendDataCallback
		{
			get { return m_prop.SendData; }
			set { m_prop.SendData = value; }
		}

		public static void AxisDown(Point point)
		{
			if (m_prop.Profile.AxisCenter.HasValue) {
				m_input[Client.Type].AxisDown(point, m_prop);
			}
		}

		public static void AxisUpdate(Point point)
		{
			if (m_prop.Profile.AxisCenter.HasValue) {
				m_input[Client.Type].AxisUpdate(point, m_prop);
			}
		}

		public static void AxisUp()
		{
			if (m_prop.Profile.AxisCenter.HasValue) {
				m_input[Client.Type].AxisUp(m_prop);
			}
		}

		public static void DirectionDown(Point point)
		{
			if (m_prop.Profile.DirectionCenter.HasValue) {
				m_input[Client.Type].DirectionDown(point, m_prop);
			}
		}

		public static void DirectionUpdate(Point point)
		{
			if (m_prop.Profile.DirectionCenter.HasValue) {
				m_input[Client.Type].DirectionUpdate(point, m_prop);
			}
		}

		public static void DirectionUp()
		{
			if (m_prop.Profile.DirectionCenter.HasValue) {
				m_input[Client.Type].DirectionUp(m_prop);
			}
		}

		public static void ButtonDown(XInput.GamePadButton button)
		{
			ButtonDown(Array.IndexOf(Constants.ButtonValue, button));
		}

		public static void ButtonDown(int index)
		{
			if (m_prop.Profile.ButtonPositions[index].Count > 0) {
				m_input[Client.Type].ButtonDown(index, m_prop);
			}
		}

		public static void ButtonUp(XInput.GamePadButton button)
		{
			ButtonUp(Array.IndexOf(Constants.ButtonValue, button));
		}

		public static void ButtonUp(int index)
		{
			if (m_prop.Profile.ButtonPositions[index].Count > 0) {
				m_input[Client.Type].ButtonUp(index, m_prop);
			}
		}

		public static void FrameUpdate()
		{
			m_input[Client.Type].FrameUpdate(m_prop);
		}
	}
}
