using System;
using System.Threading;
using Xbox2Android.Native;

namespace Xbox2Android
{
	static class TriggerAction
	{
#region Internal data types

		interface ITriggerAction
		{
			bool OnFrameUpdate();   //return true on complete
			void OnCancel();
		}

		sealed class ActionButtonClick : ITriggerAction
		{
			readonly XInput.GamePadButton m_button;
			int m_frame;

			public ActionButtonClick(XInput.GamePadButton button)
			{
				m_button = button;
			}

			public bool OnFrameUpdate()
			{
				switch (m_frame) {
					case 0:
						InputMapper.ButtonDown(m_button);
						++m_frame;
						return false;
					case 1:
						++m_frame;
						return false;
					case 2:
					default:
						InputMapper.ButtonUp(m_button);
						m_frame = 0;
						return true;
				}
			}

			public void OnCancel()
			{
				if (m_frame != 0) {
					InputMapper.ButtonUp(m_button);
					m_frame = 0;
				}
			}
		}

		sealed class ActionButtonClickFast : ITriggerAction
		{
			readonly XInput.GamePadButton m_button;
			bool m_isButtonDown;

			public ActionButtonClickFast(XInput.GamePadButton button)
			{
				m_button = button;
			}

			public bool OnFrameUpdate()
			{
				if (!m_isButtonDown) {
					InputMapper.ButtonDown(m_button);
					m_isButtonDown = true;
					return false;
				}
				else {
					InputMapper.ButtonUp(m_button);
					m_isButtonDown = false;
					return true;
				}
			}

			public void OnCancel()
			{
				if (m_isButtonDown) {
					InputMapper.ButtonUp(m_button);
					m_isButtonDown = false;
				}
			}
		}

		sealed class ActionWait : ITriggerAction
		{
			public int FrameCount { get; }
			int m_frame;

			public ActionWait(int frame)
			{
				FrameCount = frame;
			}

			public bool OnFrameUpdate()
			{
				++m_frame;
				if (m_frame >= FrameCount) {
					m_frame = 0;
					return true;
				}
				else {
					return false;
				}
			}

			public void OnCancel()
			{
				m_frame = 0;
			}
		}

#endregion

#region Action sequence

		static readonly ITriggerAction[] DoubleHarmony = {
			new ActionButtonClick(XInput.GamePadButton.A),
			new ActionWait(6),
			new ActionButtonClick(XInput.GamePadButton.B),
			new ActionWait(6),
		};

		static readonly ITriggerAction[] DoubleHarmonyShock = {
			new ActionButtonClick(XInput.GamePadButton.A),
			new ActionWait(1),
			new ActionButtonClick(XInput.GamePadButton.B),
			new ActionWait(1),
		};

		static readonly ITriggerAction[] TripleTriplet = {
			new ActionButtonClick(XInput.GamePadButton.Y),
			new ActionWait(4),
			new ActionButtonClick(XInput.GamePadButton.B),
			new ActionWait(4),
			new ActionButtonClick(XInput.GamePadButton.A),
			new ActionWait(4),
		};

		static readonly ITriggerAction[] TripleTripletFrenzy = {
			new ActionButtonClick(XInput.GamePadButton.Y),
			new ActionWait(1),
			new ActionButtonClick(XInput.GamePadButton.B),
			new ActionWait(1),
			new ActionButtonClick(XInput.GamePadButton.A),
			new ActionWait(1),
		};

		static readonly ITriggerAction[] TriggerHappyA = {
			new ActionButtonClickFast(XInput.GamePadButton.A),
		};

		static readonly ITriggerAction[] TriggerHappyFrenzyA = {
			new ActionButtonClickFast(XInput.GamePadButton.A),
		};

		static readonly ITriggerAction[] TriggerHappyB = {
			new ActionButtonClickFast(XInput.GamePadButton.B),
		};

		static readonly ITriggerAction[] TriggerFrenzyB = {
			new ActionButtonClickFast(XInput.GamePadButton.B),
		};

		static readonly ITriggerAction[][] Actions = {
			DoubleHarmony,
			DoubleHarmonyShock,
			TripleTriplet,
			TripleTripletFrenzy,
			TriggerHappyA,
			TriggerHappyFrenzyA,
			TriggerHappyB,
			TriggerFrenzyB,
		};

		public static readonly float[] ActionInterval = {
			1 / 30.0f,
			1 / 30.0f,
			1 / 30.0f,
			1 / 30.0f,
			1 / 30.0f,
			1 / 60.0f,
			1 / 30.0f,
			1 / 60.0f,
		};

#endregion

		static int m_currentIndex;

		public static void TriggerDown(int triggerMode)
		{
			if (Actions[triggerMode][m_currentIndex].OnFrameUpdate()) {
				++m_currentIndex;
				m_currentIndex %= Actions[triggerMode].Length;
			}
		}

		public static void TriggerUp(int triggerMode)
		{
			Actions[triggerMode][m_currentIndex].OnCancel();
			m_currentIndex = 0;
		}
	}
}
