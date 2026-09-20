using System;
using System.Runtime.InteropServices;
using System.Threading;

class KeySimulator
{
	[DllImport("user32.dll")]
	static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

	const int KEYEVENTF_KEYUP = 0x0002;

	const byte VK_MENU = 0x12; // Alt
	const byte VK_TAB = 0x09;  // Tab

	public static void AltTab()
	{
		// Tekan Alt
		keybd_event(VK_MENU, 0, 0, 0);

		// Tekan Tab
		keybd_event(VK_TAB, 0, 0, 0);

		// Lepas Tab
		keybd_event(VK_TAB, 0, KEYEVENTF_KEYUP, 0);

		// Lepas Alt
		keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);
	}
}