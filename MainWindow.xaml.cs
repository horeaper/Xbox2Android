using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using XboxInputMapper.Native;

namespace XboxInputMapper
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		internal static ProgramSettings Settings { get; private set; }

		IntPtr m_gameWindow = IntPtr.Zero;
		DispatcherTimer m_timer = new DispatcherTimer();

		const int ThumbDeadzone = short.MaxValue / 2;
		const int TriggerDeadzone = byte.MaxValue / 4;
		const int MaxTouchCount = 10;
		XInput.Gamepad m_previousGamepad;
		bool m_isDirectionInEffect;
		bool m_isLeftTriggerDown;
		bool m_isRightTriggerDown;
		bool m_isShadowAxis;

		readonly Dictionary<Point, int> m_posMap = new Dictionary<Point, int>();

		public MainWindow()
		{
			InitializeComponent();

			Settings = ProgramSettings.Load();
			textAdbPath.Text = Settings.AdbPath;
			checkTriggerHappy.IsChecked = Settings.IsTriggerHappy;
			RefreshPositionIndex();
			InitializeNotifyIcon();
			ReconnectAdb();
		}

		void RefreshPositionIndex()
		{
			m_posMap.Clear();
			int index = 0;
			foreach (var positions in Settings.ButtonPositions) {
				foreach (var point in positions) {
					m_posMap.Add(point, index++);
				}
			}
			foreach (var point in Settings.LeftTriggerPositions) {
				m_posMap.Add(point, index++);
			}
			foreach (var point in Settings.RightTriggerPositions) {
				m_posMap.Add(point, index++);
			}
		}

		void ResetGamepadState()
		{
			m_previousGamepad = new XInput.Gamepad();
			m_isDirectionInEffect = false;
			m_isLeftTriggerDown = false;
			m_isRightTriggerDown = false;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			m_timer.Tick += timer_Tick;
			m_timer.Interval = TimeSpan.FromSeconds(1.0 / 60.0);
			m_timer.Start();

			WindowState = Settings.IsMinimized ? WindowState.Minimized : WindowState.Normal;
			MainWindow_StateChanged(null, null);
		}

		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			Settings.IsMinimized = WindowState == WindowState.Minimized;
			Settings.AdbPath = textAdbPath.Text;
			Settings.LastSelectedDevice = ((ComboBoxItem)comboDevices.SelectedItem)?.Content.ToString();

		tagRetry:
			if (!Settings.Save()) {
				var result = MessageBox.Show(this, "Cannot save program settings. Retry?", "Xbox Input Mapper", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
				if (result == MessageBoxResult.Yes) {
					goto tagRetry;
				}
			}

			m_timer.Stop();
			Environment.Exit(0);
		}

		private void MainWindow_StateChanged(object sender, EventArgs e)
		{
			if (WindowState == WindowState.Minimized) {
				Hide();
				m_notifyIcon.Visible = true;
			}
		}

		private void CheckTriggerHappy_CheckedChanged(object sender, RoutedEventArgs e)
		{
			Settings.IsTriggerHappy = checkTriggerHappy.IsChecked == true;
			ResetGamepadState();
		}

		private void CheckReverseAxis_CheckedChanged(object sender, RoutedEventArgs e)
		{
			Settings.IsReverseAxis = checkReverseAxis.IsChecked == true;
		}

		private void ProfileButton_Click(object sender, RoutedEventArgs e)
		{
			var profileWindow = new TouchProfileWindow();
			profileWindow.Owner = this;
			profileWindow.ShowDialog();
			RefreshPositionIndex();
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			XInput.State state;
			if (XInput.GetState(0, out state) == XInput.ErrorSuccess && m_inputDevicePath != null) {
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
					if (!gamepad.Buttons.HasFlag(value)) {	//No button
						if (isButtonInEffect) {
							foreach (var point in Settings.ButtonPositions[buttonId]) {
								DoTouchUp(m_posMap[point]);
							}
						}
					}
					else {
						if (!isButtonInEffect) {
							foreach (var point in Settings.ButtonPositions[buttonId]) {
								DoTouchDown(m_posMap[point], point);
							}
						}
						else {
							foreach (var point in Settings.ButtonPositions[buttonId]) {
								DoTouchUpdate(m_posMap[point], point);
							}
						}
					}
				}

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
