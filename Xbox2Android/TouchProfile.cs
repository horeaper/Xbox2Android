using System.Collections.Generic;
using System.Windows;
using System.Xml.Linq;

namespace Xbox2Android
{
	public class TouchProfile
	{
		public string Name;
		public string BackgroundImage;
		public float Width;
		public float Height;

		public int TriggerMode;
		public int TriggerHappyValue;
		public int TriggerDoubleValue;
		public int TriggerTripleValue;

		public bool IsReverseAxis;
		public bool IsSnapAxis;

		public bool IsHotKey;

		public Point? AxisCenter;
		public int AxisRadius = 120;
		public int? ShadowAxisOffset;
		public List<Point>[] ButtonPositions = new List<Point>[Constants.ButtonCount];

		public TouchProfile()
		{
			for (int cnt = 0; cnt < Constants.ButtonCount; ++cnt) {
				ButtonPositions[cnt] = new List<Point>();
			}
		}

		public bool Load(XElement rootElement)
		{
			try {
				if (rootElement.Attribute("Name") != null) {
					Name = rootElement.Attribute("Name").Value;
				}
				if (rootElement.Attribute("BackgroundImage") != null) {
					BackgroundImage = rootElement.Attribute("BackgroundImage").Value;
				}
				if (rootElement.Attribute("Width") != null) {
					Width = int.Parse(rootElement.Attribute("Width").Value);
				}
				if (rootElement.Attribute("Height") != null) {
					Height = int.Parse(rootElement.Attribute("Height").Value);
				}

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

				if (rootElement.Attribute("IsReverseAxis") != null) {
					IsReverseAxis = bool.Parse(rootElement.Attribute("IsReverseAxis").Value);
				}
				if (rootElement.Attribute("IsSnapAxis") != null) {
					IsSnapAxis = bool.Parse(rootElement.Attribute("IsSnapAxis").Value);
				}

				if (rootElement.Attribute("IsHotKey") != null) {
					IsHotKey = bool.Parse(rootElement.Attribute("IsHotKey").Value);
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

				return true;
			}
			catch {
				return false;
			}
		}

		public XElement Save()
		{
			try {
				var rootElement = new XElement("Settings");
				if (Name != null) {
					rootElement.SetAttributeValue("Name", Name);
				}
				rootElement.SetAttributeValue("BackgroundImage", BackgroundImage);
				rootElement.SetAttributeValue("Width", Width);
				rootElement.SetAttributeValue("Height", Height);

				rootElement.SetAttributeValue("TriggerMode", TriggerMode);
				rootElement.SetAttributeValue("TriggerHappyValue", TriggerHappyValue);
				rootElement.SetAttributeValue("TriggerDoubleValue", TriggerDoubleValue);
				rootElement.SetAttributeValue("TriggerTripleValue", TriggerTripleValue);

				rootElement.SetAttributeValue("IsReverseAxis", IsReverseAxis);
				rootElement.SetAttributeValue("IsSnapAxis", IsSnapAxis);

				rootElement.SetAttributeValue("IsHotKey", IsHotKey);

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

				return rootElement;
			}
			catch {
				return null;
			}
		}

		public void AssignIfNoName(int index)
		{
			if (string.IsNullOrEmpty(Name)) {
				Name = index.ToString();
			}
		}
	}
}
