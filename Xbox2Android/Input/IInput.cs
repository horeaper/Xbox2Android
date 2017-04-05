using System;
using System.Windows;
using Xbox2Android.Native;

namespace Xbox2Android.Input
{
	class Property
	{
		public MainWindow.ClientParam Client;
		public TouchProfile Profile;
		public Action<byte[]> SendData;
	}

	interface IInput
	{
		void AxisDown(Point point, Property prop);
		void AxisUpdate(Point point, Property prop);
		void AxisUp(Property prop);

		void DirectionDown(Point point, Property prop);
		void DirectionUpdate(Point point, Property prop);
		void DirectionUp(Property prop);

		void ButtonDown(int index, Property prop);
		void ButtonUp(int index, Property prop);

		void FrameUpdate(Property prop);
	}
}
