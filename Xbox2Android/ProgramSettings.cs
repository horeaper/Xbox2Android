using System.Collections.Generic;
using System.Windows;
using System.Xml.Linq;

namespace Xbox2Android
{
	static class ProgramSettings
	{
		public static bool IsMinimized;

		public static int TriggerMode;
		public static bool IsReverseAxis;

		public static Point? AxisCenter;
		public static int AxisRadius = 120;
		public static int ShadowAxisOffset = -8;

		public static string BackgroundImage;
		public static List<Point>[] ButtonPositions = new List<Point>[Constants.ButtonCount];

		public static void Load()
		{
			try {
				var doc = XDocument.Load("InputSettings.xml");
				var rootElement = doc.Root;

				IsMinimized = bool.Parse(rootElement.Attribute("IsMinimized").Value);
				TriggerMode = int.Parse(rootElement.Attribute("TriggerMode").Value);
				IsReverseAxis = bool.Parse(rootElement.Attribute("IsReverseAxis").Value);
				if (rootElement.Attribute("AxisCenter") != null) {
					AxisCenter = Point.Parse(rootElement.Attribute("AxisCenter").Value);
				}
				AxisRadius = int.Parse(rootElement.Attribute("AxisRadius").Value);
				ShadowAxisOffset = int.Parse(rootElement.Attribute("ShadowAxisOffset").Value);
				BackgroundImage = rootElement.Attribute("BackgroundImage").Value;

				int index = 0;
				foreach (var buttonElement in rootElement.Elements("Button")) {
					ButtonPositions[index] = new List<Point>();
					foreach (var pointElement in buttonElement.Elements("Point")) {
						var point = Point.Parse(pointElement.Attribute("Value").Value);
						ButtonPositions[index].Add(point);
					}
					++index;
				}
			}
			catch {
				// ignored
			}
		}

		public static bool Save()
		{
			try {
				var rootElement = new XElement("Settings");
				rootElement.SetAttributeValue("IsMinimized", IsMinimized);
				rootElement.SetAttributeValue("TriggerMode", TriggerMode);
				rootElement.SetAttributeValue("IsReverseAxis", IsReverseAxis);
				if (AxisCenter.HasValue) {
					rootElement.SetAttributeValue("AxisCenter", AxisCenter.Value);
				}
				rootElement.SetAttributeValue("AxisRadius", AxisRadius);
				rootElement.SetAttributeValue("ShadowAxisOffset", ShadowAxisOffset);
				rootElement.SetAttributeValue("BackgroundImage", BackgroundImage);

				for (int cnt = 0; cnt < Constants.ButtonCount; ++cnt) {
					var buttonElement = new XElement("Button");
					foreach (var point in ButtonPositions[cnt]) {
						var pointElement = new XElement("Point");
						pointElement.SetAttributeValue("Value", point);
						buttonElement.Add(pointElement);
					}
					rootElement.Add(buttonElement);
				}

				var doc = new XDocument();
				doc.Add(rootElement);
				doc.Save("InputSettings.xml");
			}
			catch {
				return false;
			}

			return true;
		}
	}
}
