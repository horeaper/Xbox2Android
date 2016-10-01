using System.Collections.Generic;
using System.Windows;
using System.Xml.Linq;

namespace Xbox2Android
{
	static class ProgramSettings
	{
		public static string BackgroundImage;

		public static int TriggerMode;
		public static int TriggerHappyValue;
		public static int TriggerDoubleValue;
		public static int TriggerTripleValue;

		public static bool IsHotKey;
		public static bool IsReverseAxis;
		public static bool IsSnapAxis;
		public static bool Is8Axis;

		public static Point? AxisCenter;
		public static int AxisRadius = 120;
		public static int? ShadowAxisOffset;
		public static List<Point>[] ButtonPositions = new List<Point>[Constants.ButtonCount];

		public static void Load()
		{
			for (int cnt = 0; cnt < Constants.ButtonCount; ++cnt) {
				ButtonPositions[cnt] = new List<Point>();
			}
			try {
				var doc = XDocument.Load("InputSettings.xml");
				var rootElement = doc.Root;

				BackgroundImage = rootElement.Attribute("BackgroundImage").Value;

				if (rootElement.Attribute("TriggerMode") != null) {
					TriggerMode = int.Parse(rootElement.Attribute("TriggerMode").Value);
				}
				if (rootElement.Attribute("TriggerHappyValue") != null) {
					TriggerHappyValue = int.Parse(rootElement.Attribute("TriggerHappyValue").Value);
				}
				if (rootElement.Attribute("TriggerDoubleValue") != null) {
					TriggerDoubleValue = int.Parse(rootElement.Attribute("TriggerDoubleValue").Value);
				}
				if (rootElement.Attribute("TriggerTripleValue") != null) {
					TriggerTripleValue = int.Parse(rootElement.Attribute("TriggerTripleValue").Value);
				}

				if (rootElement.Attribute("IsHotKey") != null) {
					IsHotKey = bool.Parse(rootElement.Attribute("IsHotKey").Value);
				}
				if (rootElement.Attribute("IsReverseAxis") != null) {
					IsReverseAxis = bool.Parse(rootElement.Attribute("IsReverseAxis").Value);
				}
				if (rootElement.Attribute("IsSnapAxis") != null) {
					IsSnapAxis = bool.Parse(rootElement.Attribute("IsSnapAxis").Value);
				}
				if (rootElement.Attribute("Is8Axis") != null) {
					Is8Axis = bool.Parse(rootElement.Attribute("Is8Axis").Value);
				}

				if (rootElement.Attribute("AxisCenter") != null) {
					AxisCenter = Point.Parse(rootElement.Attribute("AxisCenter").Value);
				}
				AxisRadius = int.Parse(rootElement.Attribute("AxisRadius").Value);
				if (rootElement.Attribute("ShadowAxisOffset") != null) {
					ShadowAxisOffset = int.Parse(rootElement.Attribute("ShadowAxisOffset").Value);
				}
				int index = 0;
				foreach (var buttonElement in rootElement.Elements("Button")) {
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
				rootElement.SetAttributeValue("BackgroundImage", BackgroundImage);

				rootElement.SetAttributeValue("TriggerMode", TriggerMode);
				rootElement.SetAttributeValue("TriggerHappyValue", TriggerHappyValue);
				rootElement.SetAttributeValue("TriggerDoubleValue", TriggerDoubleValue);
				rootElement.SetAttributeValue("TriggerTripleValue", TriggerTripleValue);

				rootElement.SetAttributeValue("IsHotKey", IsHotKey);
				rootElement.SetAttributeValue("IsReverseAxis", IsReverseAxis);
				rootElement.SetAttributeValue("IsSnapAxis", IsSnapAxis);
				rootElement.SetAttributeValue("Is8Axis", Is8Axis);

				if (AxisCenter.HasValue) {
					rootElement.SetAttributeValue("AxisCenter", AxisCenter.Value);
				}
				rootElement.SetAttributeValue("AxisRadius", AxisRadius);
				if (ShadowAxisOffset.HasValue) {
					rootElement.SetAttributeValue("ShadowAxisOffset", ShadowAxisOffset.Value);
				}
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
