using System.Windows;
using Xbox2Android.Native;

namespace Xbox2Android
{
	static class Constants
	{
		public const int ButtonCount = 14;

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

		public static readonly string[] BalloonLevelDisplay = {
			"Low", "High"
		};

		//     0
		//   7   1
		// 6       2
		//   5   3
		//     4
		public static readonly Vector[] DirectionVector = new Vector[8];

		static Constants()
		{
			//     1
			// -1  0  1
			//    -1
			DirectionVector[0] = new Vector(0, 1);
			DirectionVector[1] = new Vector(1, 1);
			DirectionVector[2] = new Vector(1, 0);
			DirectionVector[3] = new Vector(1, -1);
			DirectionVector[4] = new Vector(0, -1);
			DirectionVector[5] = new Vector(-1, -1);
			DirectionVector[6] = new Vector(-1, 0);
			DirectionVector[7] = new Vector(-1, 1);
			for (int cnt = 0; cnt < DirectionVector.Length; ++cnt) {
				DirectionVector[cnt].Normalize();
			}
		}
	}
}
