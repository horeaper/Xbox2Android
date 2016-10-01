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
		bool m_isTriggerDown = false;

		void ResetGamepadState()
		{
			m_previousGamepad = new XInput.Gamepad();
			m_isDirectionInEffect = false;
		}

		private void Timer_Tick()
		{
			XInput.State state;
			if (XInput.GetState(0, out state) == XInput.ErrorSuccess) {
				int triggerValue = 0;
				switch (ProgramSettings.TriggerMode) {
					case 0:
						triggerValue = ProgramSettings.TriggerHappyValue;
						break;
					case 1:
						triggerValue = ProgramSettings.TriggerDoubleValue;
						break;
					case 2:
						triggerValue = ProgramSettings.TriggerTripleValue;
						break;
				}

				//Button
				if (!ProcessButton(state.Gamepad)) {
					//If button is not handled, process trigger
					if (state.Gamepad.RightTrigger > TriggerDeadzone) {
						RightTriggerAction.TriggerDown(ProgramSettings.TriggerMode, triggerValue);
						m_isTriggerDown = true;
					}
					else {
						if (m_previousGamepad.RightTrigger > TriggerDeadzone) {
							RightTriggerAction.TriggerUp(ProgramSettings.TriggerMode, triggerValue);
							m_isTriggerDown = false;
						}
					}
				}
				else {
					//If button is pressed while trigger is still processing, cancel it
					if (m_isTriggerDown) {
						RightTriggerAction.TriggerUp(ProgramSettings.TriggerMode, triggerValue);
						m_isTriggerDown = false;
					}
				}

				//Axis
				ProcessAxis(state.Gamepad);

				//Refresh
				m_previousGamepad = state.Gamepad;
				if (m_selectedClient != null) {
					InputMapper.FrameUpdate(m_selectedClient, SendData);
				}
			}
			else {
				ResetGamepadState();
			}
		}

		bool m_isDirectionInEffect;
		bool m_isShadowAxisTriggered;

		bool ProcessButton(XInput.Gamepad gamepad)
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

		void ProcessAxis(XInput.Gamepad gamepad)
		{
			if (ProgramSettings.AxisCenter.HasValue && ProgramSettings.AxisRadius > 0) {
				var direction = new Vector(gamepad.ThumbLX, gamepad.ThumbLY);
				if (direction.Length <= ThumbDeadzone) {
					direction.X = 0;
					direction.Y = 0;
				}
				if (ProgramSettings.IsSnapAxis) {
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
					if (ProgramSettings.Is8Axis) {
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
					direction *= ProgramSettings.AxisRadius;

					//Reverse axis
					if (ProgramSettings.IsReverseAxis) {
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
					var axisCenter = ProgramSettings.AxisCenter.Value;
					if (m_isShadowAxisTriggered && ProgramSettings.ShadowAxisOffset.HasValue) {
						axisCenter.X += ProgramSettings.ShadowAxisOffset.Value;
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
