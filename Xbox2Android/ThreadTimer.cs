using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Xbox2Android
{
	class ThreadTimer
	{
		[DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		static extern bool QueryPerformanceFrequency(out long frequency);
		[DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		static extern bool QueryPerformanceCounter(out long counter);

		readonly long m_frequency;
		long m_lastCounter;
		readonly Action m_callback;
		volatile float m_interval;
		readonly Thread m_runningThread;
		volatile bool m_isRunning = true;

		public ThreadTimer(Action callback, float interval)
		{
			QueryPerformanceFrequency(out m_frequency);
			QueryPerformanceCounter(out m_lastCounter);
			m_callback = callback;
			m_interval = interval;
			m_runningThread = new Thread(ThreadEntry);
			m_runningThread.Start();
		}

		public void Change(float interval)
		{
			m_interval = interval;
		}

		public void Stop()
		{
			m_isRunning = false;
			m_runningThread.Join();
		}

		void ThreadEntry()
		{
			while (m_isRunning) {
				long counter;
				QueryPerformanceCounter(out counter);
				double elapsed = (double)(counter - m_lastCounter) / m_frequency;
				if (elapsed >= m_interval) {
					m_callback();
					m_lastCounter = counter;
				}
				else {
					Thread.Sleep(1);
				}
			}
		}
	}
}
