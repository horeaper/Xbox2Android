using System;
using System.Windows;
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
			if (m_currentProfile != -1 && XInput.GetState(0, out state) == XInput.ErrorSuccess) {
				var profile = m_profiles[m_currentProfile];

				int triggerValue = 0;
				switch (profile.TriggerMode) {
					case 0:
						triggerValue = profile.TriggerHappyValue;
						break;
					case 1:
						triggerValue = profile.TriggerDoubleValue;
						break;
					case 2:
						triggerValue = profile.TriggerTripleValue;
						break;
				}

				//Button
				if (!ProcessButton(profile, state.Gamepad)) {
					//If button is not handled, process trigger
					if (state.Gamepad.RightTrigger > TriggerDeadzone) {
						RightTriggerAction.TriggerDown(profile.TriggerMode, triggerValue);
						m_isTriggerDown = true;
					}
					else {
						if (m_previousGamepad.RightTrigger > TriggerDeadzone) {
							RightTriggerAction.TriggerUp(profile.TriggerMode, triggerValue);
							m_isTriggerDown = false;
						}
					}
				}
				else {
					//If button is pressed while trigger is still processing, cancel it
					if (m_isTriggerDown) {
						RightTriggerAction.TriggerUp(profile.TriggerMode, triggerValue);
						m_isTriggerDown = false;
					}
				}

				//Axis
				ProcessAxis(profile, state.Gamepad);

				//Refresh
				m_previousGamepad = state.Gamepad;
				if (m_selectedClient != null) {
					InputMapper.FrameUpdate(profile, m_selectedClient, SendData);
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
			for (int buttonId = 0; buttonId < Constants.ButtonValue.Length; ++buttonId) {
				var flag = Constants.ButtonValue[buttonId];
				bool isPreviousButtonDown = m_previousGamepad.Buttons.HasFlag(flag);
				if (gamepad.Buttons.HasFlag(flag)) {
					isHandled = true;
					if (!isPreviousButtonDown) {
						InputMapper.ButtonDown(buttonId);
					}
				}
				else {
					if (isPreviousButtonDown) {
						InputMapper.ButtonUp(buttonId);
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
					//8-axis
					if (profile.Is8Axis) {
						var angle = Math.Abs(Math.Atan(direction.Y / direction.X) * (180 / Math.PI));
						if (angle > 0 && angle <= 22.5) {
							angle = 0;
						}
						else if (angle >= 22.5 && angle <= 67.5) {
							angle = 45;
						}
						else if (angle >= 45 + 22.5 && angle < 90) {
							angle = 90;
						}
						if (direction.X > 0) {
							if (angle == 0) {
								direction = new Vector(1, 0);
							}
							else if (angle == 45) {
								direction = direction.Y > 0 ? new Vector(1, 1) : new Vector(1, -1);
							}
							else if (angle == 90) {
								direction = direction.Y > 0 ? new Vector(0, 1) : new Vector(0, -1);
							}
						}
						else {
							if (angle == 0) {
								direction = new Vector(-1, 0);
							}
							else if (angle == 45) {
								direction = direction.Y > 0 ? new Vector(-1, 1) : new Vector(-1, -1);
							}
							else if (angle == 90) {
								direction = direction.Y > 0 ? new Vector(0, 1) : new Vector(0, -1);
							}
						}
					}

					//Normalize
					direction.Normalize();
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
