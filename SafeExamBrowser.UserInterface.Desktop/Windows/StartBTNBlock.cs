using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SafeExamBrowser.UserInterface.Desktop.Windows
{
	public class WinKeyBlocker
	{
		private static IntPtr hookId = IntPtr.Zero;
		private static LowLevelKeyboardProc proc = HookCallback;

		public static void EnableBlock()
		{
			hookId = SetHook(proc);
		}

		public static void DisableBlock()
		{
			UnhookWindowsHookEx(hookId);
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

				// Block Left Win dan Right Win
				if (vkCode == 0x5B || vkCode == 0x5C)
				{
					return (IntPtr) 1;
				}
			}

			return CallNextHookEx(hookId, nCode, wParam, lParam);
		}

		[DllImport("user32.dll")]
		private static extern IntPtr SetWindowsHookEx(
			int idHook,
			LowLevelKeyboardProc lpfn,
			IntPtr hMod,
			uint dwThreadId);

		[DllImport("user32.dll")]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll")]
		private static extern IntPtr CallNextHookEx(
			IntPtr hhk,
			int nCode,
			IntPtr wParam,
			IntPtr lParam);

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
	}
}
