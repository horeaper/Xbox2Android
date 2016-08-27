using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using XboxInputMapper.Native;

namespace XboxInputMapper
{
	partial class MainWindow
	{
		const int ThumbDeadzone = short.MaxValue / 4;
		const int TriggerDeadzone = byte.MaxValue / 2;
		const int MaxTouchCount = 10;
		XInput.Gamepad m_previousGamepad;
		bool m_isDirectionInEffect;
		bool m_isLeftTriggerDown;
		bool m_isRightTriggerDown;
		bool m_isShadowAxis;

		void RefreshPositionIndex()
		{
			m_posIndexMap.Clear();
			int index = 0;
			foreach (var positions in Settings.ButtonPositions) {
				foreach (var point in positions) {
					m_posIndexMap.Add(point, index++);
				}
			}
		}

		void ResetGamepadState()
		{
			m_previousGamepad = new XInput.Gamepad();
			m_isDirectionInEffect = false;
			m_isLeftTriggerDown = false;
			m_isRightTriggerDown = false;
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
							DoTouchUp(MaxTouchCount - 1);
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
							DoTouchDown(MaxTouchCount - 1, point);
							m_isDirectionInEffect = true;
						}
						else {
							DoTouchUpdate(MaxTouchCount - 1, point);
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
								DoTouchUp(m_posIndexMap[point]);
							}
						}
					}
					else {
						if (!isButtonInEffect) {
							foreach (var point in Settings.ButtonPositions[buttonId]) {
								DoTouchDown(m_posIndexMap[point], point);
							}
						}
						else {
							foreach (var point in Settings.ButtonPositions[buttonId]) {
								DoTouchUpdate(m_posIndexMap[point], point);
							}
						}
					}
				}

/*
				if (Settings.IsTriggerHappy) {
					//Left trigger
					bool isLeftTriggerInEffect = m_previousGamepad.LeftTrigger > TriggerDeadzone;
					if (gamepad.LeftTrigger <= TriggerDeadzone) {   //No trigger
						if (isLeftTriggerInEffect) {
							if (m_isLeftTriggerDown) {
								foreach (var point in Settings.LeftTriggerPositions) {
									DoTouchUp(m_posMap[point]);
								}
							}
							m_isLeftTriggerDown = false;
						}
					}
					else {
						if (!isLeftTriggerInEffect) {
							foreach (var point in Settings.LeftTriggerPositions) {
								DoTouchDown(m_posMap[point], point);
							}
							m_isLeftTriggerDown = true;
						}
						else {
							if (m_isLeftTriggerDown) {
								foreach (var point in Settings.LeftTriggerPositions) {
									DoTouchUp(m_posMap[point]);
								}
							}
							else {
								foreach (var point in Settings.LeftTriggerPositions) {
									DoTouchDown(m_posMap[point], point);
								}
							}
							m_isLeftTriggerDown = !m_isLeftTriggerDown;
						}
					}

					//Right trigger
					bool isRightTriggerInEffect = m_previousGamepad.RightTrigger > TriggerDeadzone;
					if (gamepad.RightTrigger <= TriggerDeadzone) {   //No trigger
						if (isRightTriggerInEffect) {
							if (m_isRightTriggerDown) {
								foreach (var point in Settings.RightTriggerPositions) {
									DoTouchUp(m_posMap[point]);
								}
							}
							m_isRightTriggerDown = false;
						}
					}
					else {
						if (!isRightTriggerInEffect) {
							foreach (var point in Settings.RightTriggerPositions) {
								DoTouchDown(m_posMap[point], point);
							}
							m_isRightTriggerDown = true;
						}
						else {
							if (m_isRightTriggerDown) {
								foreach (var point in Settings.RightTriggerPositions) {
									DoTouchUp(m_posMap[point]);
								}
							}
							else {
								foreach (var point in Settings.RightTriggerPositions) {
									DoTouchDown(m_posMap[point], point);
								}
							}
							m_isRightTriggerDown = !m_isRightTriggerDown;
						}
					}
				}
				else {
					//Left trigger
					bool isLeftTriggerInEffect = m_previousGamepad.LeftTrigger > TriggerDeadzone;
					if (gamepad.LeftTrigger <= TriggerDeadzone) {   //No trigger
						if (isLeftTriggerInEffect) {
							foreach (var point in Settings.LeftTriggerPositions) {
								DoTouchUp(m_posMap[point]);
							}
						}
					}
					else {
						if (!isLeftTriggerInEffect) {
							foreach (var point in Settings.LeftTriggerPositions) {
								DoTouchDown(m_posMap[point], point);
							}
						}
						else {
							foreach (var point in Settings.LeftTriggerPositions) {
								DoTouchUpdate(m_posMap[point], point);
							}
						}
					}

					//Right trigger
					bool isRightTriggerInEffect = m_previousGamepad.RightTrigger > TriggerDeadzone;
					if (gamepad.RightTrigger <= TriggerDeadzone) {   //No trigger
						if (isRightTriggerInEffect) {
							foreach (var point in Settings.RightTriggerPositions) {
								DoTouchUp(m_posMap[point]);
							}
						}
					}
					else {
						if (!isRightTriggerInEffect) {
							foreach (var point in Settings.RightTriggerPositions) {
								DoTouchDown(m_posMap[point], point);
							}
						}
						else {
							foreach (var point in Settings.RightTriggerPositions) {
								DoTouchUpdate(m_posMap[point], point);
							}
						}
					}
				}
*/

				m_previousGamepad = gamepad;

				SendTouchData();
			}
			else {
				m_previousGamepad = new XInput.Gamepad();
				m_isDirectionInEffect = false;
				m_isLeftTriggerDown = false;
				m_isRightTriggerDown = false;
			}
		}
	}
}
