using System;
using System.Windows;
using XboxInputMapper.Native;

namespace XboxInputMapper
{
	partial class MainWindow
	{
		const int ThumbDeadzone = short.MaxValue / 4;
		const int TriggerDeadzone = byte.MaxValue / 2;
		const int AxisIndex = Constants.ButtonCount;
		XInput.Gamepad m_previousGamepad;
		bool m_isDirectionInEffect;
		bool m_isShadowAxis;

		void ResetGamepadState()
		{
			m_previousGamepad = new XInput.Gamepad();
			m_isDirectionInEffect = false;
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			XInput.State state;
			if (XInput.GetState(0, out state) == XInput.ErrorSuccess && m_inputEventPath != null) {
				//Axis
				if (Settings.AxisCenter.HasValue && Settings.AxisRadius > 0) {
					var direction = new Vector(state.Gamepad.ThumbLX, state.Gamepad.ThumbLY);
					if (Math.Abs(direction.X) <= ThumbDeadzone) {
						direction.X = 0;
					}
					if (Math.Abs(direction.Y) <= ThumbDeadzone) {
						direction.Y = 0;
					}
					if (direction.X == 0 && direction.Y == 0) {    //No direction
						if (m_isDirectionInEffect) {
							DoTouchUp(AxisIndex);
							m_isDirectionInEffect = false;
						}
					}
					else {
						direction.Normalize();
						direction *= Settings.AxisRadius;

						//Reverse axis
						if (Settings.IsReverseAxis) {
							direction.X = -direction.X;
							direction.Y = -direction.Y;
						}

						//Shadow axis
						if (direction.X > 0) {
							m_isShadowAxis = false;
						}
						else if (direction.X < 0) {
							m_isShadowAxis = true;
						}
						var axisCenter = Settings.AxisCenter.Value;
						if (m_isShadowAxis) {
							axisCenter.X += Settings.ShadowAxisOffset;
						}

						//Output
						var point = new Point(axisCenter.X + direction.X, axisCenter.Y - direction.Y);
						if (!m_isDirectionInEffect) {
							DoTouchDown(AxisIndex, point);
							m_isDirectionInEffect = true;
						}
						else {
							DoTouchUpdate(AxisIndex, point);
						}
					}
				}

				var gamepad = state.Gamepad;

				//Button
				for (int buttonId = 0; buttonId < Constants.ButtonValue.Length; ++buttonId) {
					var value = Constants.ButtonValue[buttonId];
					bool isButtonInEffect = m_previousGamepad.Buttons.HasFlag(value);
					if (!gamepad.Buttons.HasFlag(value)) {  //No button
						if (isButtonInEffect) {
							foreach (var point in Settings.ButtonPositions[buttonId]) {
								DoTouchUp(buttonId);
							}
						}
					}
					else {
						if (!isButtonInEffect) {
							foreach (var point in Settings.ButtonPositions[buttonId]) {
								DoTouchDown(buttonId, point);
							}
						}
					}
				}

				m_previousGamepad = gamepad;
				SendTouchData();
			}
			else {
				ResetGamepadState();
			}
		}
	}
}
