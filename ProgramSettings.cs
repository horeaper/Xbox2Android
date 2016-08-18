using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;

namespace XboxInputMapper
{
	[Serializable]
	class ProgramSettings
	{
		public bool IsMinimized = false;

		public string AdbPath = "";
		public string LastSelectedDevice;

		public bool IsTriggerHappy = false;
		public bool IsReverseAxis = false;

		public Point? AxisCenter;
		public int AxisRadius = 120;
		public int ShadowAxisOffset = -8;

		public string BackgroundImage;
		public List<Point>[] ButtonPositions = new List<Point>[Constants.ButtonCount];
		public List<Point> LeftTriggerPositions = new List<Point>();
		public List<Point> RightTriggerPositions = new List<Point>();

		public static ProgramSettings Load()
		{
			try {
				using (var inputFile = new FileStream("InputSettings.bin", FileMode.Open, FileAccess.Read, FileShare.Read)) {
					return new BinaryFormatter().Deserialize(inputFile) as ProgramSettings;
				}
			}
			catch (IOException) {
			}

			var newSettings = new ProgramSettings();
			for (int cnt = 0; cnt < Constants.ButtonCount; ++cnt) {
				newSettings.ButtonPositions[cnt] = new List<Point>();
			}
			return newSettings;
		}

		public bool Save()
		{
			try {
				using (var outputFile = new FileStream("InputSettings.bin", FileMode.Create, FileAccess.Write, FileShare.Read)) {
					new BinaryFormatter().Serialize(outputFile, this);
				}
			}
			catch (IOException) {
				return false;
			}

			return true;
		}
	}
}
