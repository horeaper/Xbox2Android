using System;
using System.Windows;
using Xbox2Android.Input;
using Xbox2Android.Native;

namespace Xbox2Android
{
	partial class MainWindow
	{
		const int ThumbDeadzone = short.MaxValue / 2;
		const int TriggerDeadzone = byte.MaxValue / 2;
		XInput.Gamepad m_previousGamepad;
		bool m_isTriggerDown;

		void ResetGamepadState()
		{
			m_previousGamepad = new XInput.Gamepad();
			m_isDirectionInEffect = false;
		}

		private void Timer_Tick()
		{
			XInput.State state;
			if (m_selectedClient != null && m_selectedProfileIndex != -1 && XInput.GetState(0, out state) == XInput.ErrorSuccess) {
				int triggerValue = 0;
				switch (CurrentProfile.TriggerMode) {
					case 0:
						triggerValue = CurrentProfile.TriggerHappyValue;
						break;
					case 1:
						triggerValue = CurrentProfile.TriggerDoubleValue;
						break;
					case 2:
						triggerValue = CurrentProfile.TriggerTripleValue;
						break;
				}

				//Button
				if (!ProcessButton(CurrentProfile, state.Gamepad)) {
					//If button is not handled, process trigger
					if (state.Gamepad.RightTrigger > TriggerDeadzone) {
						RightTriggerAction.TriggerDown(CurrentProfile.TriggerMode, triggerValue);
						m_isTriggerDown = true;
					}
					else {
						if (m_previousGamepad.RightTrigger > TriggerDeadzone) {
							RightTriggerAction.TriggerUp(CurrentProfile.TriggerMode, triggerValue);
							m_isTriggerDown = false;
						}
					}
				}
				else {
					//If button is pressed while trigger is still processing, cancel it
					if (m_isTriggerDown) {
						RightTriggerAction.TriggerUp(CurrentProfile.TriggerMode, triggerValue);
						m_isTriggerDown = false;
					}
				}

				//Axis
				ProcessAxis(CurrentProfile, state.Gamepad);

				//Refresh
				m_previousGamepad = state.Gamepad;
				if (m_selectedClient != null) {
					InputMapper.FrameUpdate();
				}
			}
			else {
				ResetGamepadState();
			}
		}

		bool m_isDirectionInEffect;
		bool m_isShadowAxisTriggered;

		bool ProcessButton(TouchProfile profile, XInput.Gamepad gamepad)
		{
			bool isHandled = false;
			for (int buttonIndex = 0; buttonIndex < Constants.ButtonValue.Length; ++buttonIndex) {
				var flag = Constants.ButtonValue[buttonIndex];
				bool isPreviousButtonDown = m_previousGamepad.Buttons.HasFlag(flag);
				if (gamepad.Buttons.HasFlag(flag)) {
					isHandled = true;
					if (!isPreviousButtonDown) {
						InputMapper.ButtonDown(buttonIndex);
					}
				}
				else {
					if (isPreviousButtonDown) {
						InputMapper.ButtonUp(buttonIndex);
						isHandled = true;
					}
				}
			}
			return isHandled;
		}

		void ProcessAxis(TouchProfile profile, XInput.Gamepad gamepad)
		{
			if (profile.AxisCenter.HasValue && profile.AxisRadius > 0) {
				var direction = new Vector(gamepad.ThumbLX, gamepad.ThumbLY);
				if (direction.Length <= ThumbDeadzone) {
					direction.X = 0;
					direction.Y = 0;
				}
				if (profile.IsSnapAxis) {
					if (Math.Abs(direction.X) <= short.MaxValue / 3.0) {
						direction.X = 0;
					}
					if (Math.Abs(direction.Y) <= short.MaxValue / 3.0) {
						direction.Y = 0;
					}
				}
				if (direction.X == 0 && direction.Y == 0) {    //No direction
					if (m_isDirectionInEffect) {
						InputMapper.AxisUp();
						m_isDirectionInEffect = false;
					}
				}
				else {
					//Normalize (and apply snap)
					direction.Normalize();
					if (profile.IsSnapAxis) {
						int index = -1;
						double minValue = -1.0;
						for (int cnt = 0; cnt < Constants.DirectionVector.Length; ++cnt) {
							double dotProduct = direction * Constants.DirectionVector[cnt];
							if (dotProduct > minValue) {
								index = cnt;
								minValue = dotProduct;
							}
						}
						direction = Constants.DirectionVector[index];
					}
					direction *= profile.AxisRadius;

					//Reverse axis
					if (profile.IsReverseAxis) {
						direction.X = -direction.X;
						direction.Y = -direction.Y;
					}

					//Shadow axis
					if (direction.X > 0) {
						m_isShadowAxisTriggered = false;
					}
					else if (direction.X < 0) {
						m_isShadowAxisTriggered = true;
					}
					var axisCenter = profile.AxisCenter.Value;
					if (m_isShadowAxisTriggered && profile.ShadowAxisOffset.HasValue) {
						axisCenter.X += profile.ShadowAxisOffset.Value;
					}

					//Output
					var point = new Point(axisCenter.X + direction.X, axisCenter.Y - direction.Y);
					if (!m_isDirectionInEffect) {
						InputMapper.AxisDown(point);
						m_isDirectionInEffect = true;
					}
					else {
						InputMapper.AxisUpdate(point);
					}
				}
			}
		}
	}
}
