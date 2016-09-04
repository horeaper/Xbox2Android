using System;
using System.Runtime.InteropServices;

namespace Xbox2Android.Native
{
	static class XInput
	{
		public const string DllName = "xinput1_4.dll";

		[Flags]
		public enum GamePadButton : ushort
		{
			DPadUp        = 0x0001,
			DPadDown      = 0x0002,
			DPadLeft      = 0x0004,
			DPadRight     = 0x0008,
			Start         = 0x0010,
			Back          = 0x0020,
			LeftThumb     = 0x0040,
			RightThumb    = 0x0080,
			LeftShoulder  = 0x0100,
			RightShoulder = 0x0200,
			A             = 0x1000,
			B             = 0x2000,
			X             = 0x4000,
			Y             = 0x8000,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Gamepad
		{
			public GamePadButton Buttons;
			public byte LeftTrigger;
			public byte RightTrigger;
			public short ThumbLX;
			public short ThumbLY;
			public short ThumbRX;
			public short ThumbRY;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct State
		{
			public uint PacketNumber;
			public Gamepad Gamepad;
		}

		[DllImport(DllName, CallingConvention = CallingConvention.StdCall, EntryPoint = "XInputGetState")]
		public static extern uint GetState(int userIndex, out State state);

		public const uint ErrorSuccess = 0;
	}
}
