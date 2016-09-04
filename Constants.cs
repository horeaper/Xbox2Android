using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using XboxInputMapper.Native;

namespace XboxInputMapper
{
	static class Constants
	{
		public const int ButtonCount = 14;
		public const int TotalInputPoints = ButtonCount + 1;

		public static readonly XInput.GamePadButton[] ButtonValue = {
			XInput.GamePadButton.DPadUp,
			XInput.GamePadButton.DPadDown,
			XInput.GamePadButton.DPadLeft,
			XInput.GamePadButton.DPadRight,
			XInput.GamePadButton.Start,
			XInput.GamePadButton.Back,
			XInput.GamePadButton.LeftThumb,
			XInput.GamePadButton.RightThumb,
			XInput.GamePadButton.LeftShoulder,
			XInput.GamePadButton.RightShoulder,
			XInput.GamePadButton.A,
			XInput.GamePadButton.B,
			XInput.GamePadButton.X,
			XInput.GamePadButton.Y,
		};

		public static readonly string[] ButtonDisplayName = {
			"Up",
			"Down",
			"Left",
			"Right",
			"Start",
			"Back",
			"L3",
			"R3",
			"LB",
			"RB",
			"A",
			"B",
			"X",
			"Y"
		};
	}
}
