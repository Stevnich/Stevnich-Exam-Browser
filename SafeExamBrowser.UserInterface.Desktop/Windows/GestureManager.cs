using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

public class FocusLock
{
	private static IntPtr _hookID = IntPtr.Zero;
	private static LowLevelKeyboardProc _proc = HookCallback;

	public static void Enable()
	{
		_hookID = SetHook(_proc);
	}

	public static void Disable()
	{
		UnhookWindowsHookEx(_hookID);
	}

	private static IntPtr SetHook(LowLevelKeyboardProc proc)
	{
		using (Process curProcess = Process.GetCurrentProcess())
		using (ProcessModule curModule = curProcess.MainModule)
		{
			return SetWindowsHookEx(13, proc,
				GetModuleHandle(curModule.ModuleName), 0);
		}
	}

	private delegate IntPtr LowLevelKeyboardProc(
		int nCode, IntPtr wParam, IntPtr lParam);

	private static IntPtr HookCallback(
		int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode >= 0)
		{
			int vkCode = Marshal.ReadInt32(lParam);

			bool altPressed =
				(GetAsyncKeyState(0x12) & 0x8000) != 0;

			bool winPressed =
				(GetAsyncKeyState(0x5B) & 0x8000) != 0 ||
				(GetAsyncKeyState(0x5C) & 0x8000) != 0;

			// ALT+TAB
			if (altPressed && vkCode == 0x09)
				return (IntPtr) 1;

			// ALT+ESC
			if (altPressed && vkCode == 0x1B)
				return (IntPtr) 1;

			// WIN key shortcuts
			if (winPressed)
				return (IntPtr) 1;
		}

		return CallNextHookEx(_hookID, nCode, wParam, lParam);
	}

	[DllImport("user32.dll")]
	private static extern short GetAsyncKeyState(int vKey);

	[DllImport("user32.dll")]
	private static extern IntPtr SetWindowsHookEx(
		int idHook,
		LowLevelKeyboardProc lpfn,
		IntPtr hMod,
		uint dwThreadId);

	[DllImport("user32.dll")]
	private static extern bool UnhookWindowsHookEx(
		IntPtr hhk);

	[DllImport("user32.dll")]
	private static extern IntPtr CallNextHookEx(
		IntPtr hhk,
		int nCode,
		IntPtr wParam,
		IntPtr lParam);

	[DllImport("kernel32.dll")]
	private static extern IntPtr GetModuleHandle(
		string lpModuleName);
}