using Xbox2Android.Input;
using Xbox2Android.Native;

namespace Xbox2Android
{
	static class RightTriggerAction
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
			readonly int m_holdFrame;
			int m_frame;

			public ActionButtonClick(XInput.GamePadButton button, int holdFrame = 1)
			{
				m_button = button;
				m_holdFrame = holdFrame;
			}

			public bool OnFrameUpdate()
			{
				if (m_frame == 0) {
					InputMapper.ButtonDown(m_button);
					++m_frame;
					return false;
				}
				else if (m_frame < m_holdFrame + 1) {
					++m_frame;
					return false;
				}
				else {
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

		sealed class ActionWait : ITriggerAction
		{
			readonly int m_frameCount;
			int m_frame;

			public ActionWait(int frame)
			{
				m_frameCount = frame;
			}

			public bool OnFrameUpdate()
			{
				++m_frame;
				if (m_frame >= m_frameCount) {
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

		sealed class ActionSlide : ITriggerAction
		{
			readonly XInput.GamePadButton[] m_buttonSequence;
			int m_currentIndex;

			public ActionSlide(params XInput.GamePadButton[] buttonSequence)
			{
				m_buttonSequence = buttonSequence;
			}

			public bool OnFrameUpdate()
			{
				InputMapper.ButtonDown(m_buttonSequence[m_currentIndex]);
				int previousIndex = m_currentIndex > 0 ? m_currentIndex - 1 : m_buttonSequence.Length - 1;
				InputMapper.ButtonUp(m_buttonSequence[previousIndex]);
				++m_currentIndex;
				m_currentIndex %= m_buttonSequence.Length;
				return true;
			}

			public void OnCancel()
			{
				int previousIndex = m_currentIndex > 0 ? m_currentIndex - 1 : m_buttonSequence.Length - 1;
				InputMapper.ButtonUp(m_buttonSequence[previousIndex]);
				m_currentIndex = 0;
			}
		}

#endregion

#region Action sequence

		static readonly ITriggerAction[] DoubleHarmony = {
			new ActionButtonClick(XInput.GamePadButton.A),
			new ActionWait(6),
			new ActionButtonClick(XInput.GamePadButton.B),
			new ActionWait(7),
		};

		static readonly ITriggerAction[] DoubleHarmonyShock = {
			new ActionButtonClick(XInput.GamePadButton.A),
			new ActionWait(1),
			new ActionButtonClick(XInput.GamePadButton.B),
			new ActionWait(1),
		};

		static readonly ITriggerAction[] DoubleHarmonyMadness = {
			new ActionSlide(XInput.GamePadButton.A, XInput.GamePadButton.B),
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

		static readonly ITriggerAction[] TripleTripletMadness = {
			new ActionSlide(XInput.GamePadButton.Y, XInput.GamePadButton.B, XInput.GamePadButton.A),
		};

		static readonly ITriggerAction[] TriggerHappy = {
			new ActionButtonClick(XInput.GamePadButton.A, 0),
		};

		static readonly ITriggerAction[] TriggerHappyMadness = {
			new ActionButtonClick(XInput.GamePadButton.A, 0),
		};

		static readonly ITriggerAction[][][] Actions = {
			new[] { TriggerHappy, TriggerHappyMadness },
			new[] { DoubleHarmony, DoubleHarmonyShock, DoubleHarmonyMadness },
			new[] { TripleTriplet, TripleTripletFrenzy, TripleTripletMadness },
		};

		public static readonly float[][] ActionInterval = {
			new[] { 1 / 30.0f, 1 / 60.0f },
			new[] { 1 / 30.0f, 1 / 30.0f, 1 / 30.0f },
			new[] { 1 / 30.0f, 1 / 30.0f, 1 / 30.0f },
		};

#endregion

		static int m_currentIndex;

		public static void TriggerDown(int mode, int value)
		{
			if (Actions[mode][value][m_currentIndex].OnFrameUpdate()) {
				++m_currentIndex;
				m_currentIndex %= Actions[mode][value].Length;
			}
		}

		public static void TriggerUp(int mode, int value)
		{
			Actions[mode][value][m_currentIndex].OnCancel();
			m_currentIndex = 0;
		}
	}
}
