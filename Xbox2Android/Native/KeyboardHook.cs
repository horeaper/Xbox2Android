using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Xbox2Android.Native
{
	static class KeyboardHook
	{
		public class KeyEventArgs
		{
			public Key Key;
		}

		static IntPtr m_hookId;
		static HOOKPROC m_hookProc;
		public static event EventHandler<KeyEventArgs> KeyPressed;

		static KeyboardHook()
		{
			m_hookProc = HookCallback;
			m_hookId = SetWindowsHookEx(WH_KEYBOARD_LL, m_hookProc, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
		}

		static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0 && wParam == WM_KEYDOWN) {
				int vkCode = Marshal.ReadInt32(lParam);
				var keyValue = KeyInterop.KeyFromVirtualKey(vkCode);
				KeyPressed?.Invoke(null, new KeyEventArgs { Key = keyValue });
			}
			return CallNextHookEx(m_hookId, nCode, wParam, lParam);
		}

		const int WH_KEYBOARD_LL = 13;
		static readonly IntPtr WM_KEYDOWN = new IntPtr(0x0100);

		delegate IntPtr HOOKPROC(int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr SetWindowsHookEx(int idHook, HOOKPROC lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
	}
}
