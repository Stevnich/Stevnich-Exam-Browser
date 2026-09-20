/*
 * Copyright (c) 2026 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Microsoft.Win32;
using SafeExamBrowser.Browser.Contracts.Events;
using SafeExamBrowser.I18n.Contracts;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.UserInterface.Contracts.Shell;
using SafeExamBrowser.UserInterface.Contracts.Shell.Events;
using SafeExamBrowser.UserInterface.Shared.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static SafeExamBrowser.UserInterface.Desktop.Windows.Popup;
using MessageBox = System.Windows.Forms.MessageBox;

// If you want to disable the behavior that temporarily hides the SEB taskbar
// when the PrintScreen key is detected, uncomment the following line.
// #define DISABLE_PRINTSCREEN_HIDE

namespace SafeExamBrowser.UserInterface.Desktop.Windows
{
	internal partial class Taskbar : Window, ITaskbar
	{
		BlackWindow blackWindow;
		private readonly ILogger logger;
		Popup notificationPopup = new Popup();
		public enum DummyProctorState
		{
			None,
			Active,
			Inactive
		}
		private string previousWallpaperPath;
		private string previousWallpaperStyle;
		private string previousTileWallpaper;
		private bool wallpaperChanged;

		private bool allowClose;
		private bool isQuitButtonFocusedAtKeyDown;
		private bool isFirstChildFocusedAtKeyDown;
		private bool isCtrlLToggleOn;

		private const int HOTKEY_ID_CTRL_L = 0xB001;
		private const int WM_HOTKEY = 0x0312;
		private const uint MOD_CONTROL = 0x0002;
		private const uint VK_L = 0x4C; // 'L' key

		private HwndSource hwndSource;
		private bool hotkeyRegistered;

		private IntPtr keyboardHookHandle = IntPtr.Zero;
		private LowLevelKeyboardProc keyboardProcDelegate;
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessageTimeout(
		IntPtr hWnd,
		uint Msg,
		UIntPtr wParam,
		string lParam,
		uint fuFlags,
		uint uTimeout,
		out UIntPtr lpdwResult);

		private const uint WM_SETTINGCHANGE = 0x001A;
		private const uint SMTO_ABORTIFHUNG = 0x0002;
		private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad";

		// Always-on hook to detect PrintScreen for temporary hiding
#if !DISABLE_PRINTSCREEN_HIDE
		private IntPtr printScreenHookHandle = IntPtr.Zero;
		private LowLevelKeyboardProc printScreenProcDelegate;
		#endif

		private const int WH_KEYBOARD_LL = 13;
		private const int WM_KEYDOWN = 0x0100;
		private const int WM_SYSKEYDOWN = 0x0104;
		private const int VK_TAB = 0x09;
		private const int VK_SNAPSHOT = 0x2C;
		private const int VK_LWIN = 0x5B;
		private const int VK_RWIN = 0x5C;
		int delayMs;
		private const string BasePath = @"C:\Windows\SystemApps\";
		private const string OriginalName = "Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy";
		private const string DisabledName = "++Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy";
		private const string StartMenuContextMenuKey = @"DesktopBackground\Shell\0Toggle\shell\3Start";

		public bool ShowClock
		{
			set { Dispatcher.Invoke(() => Clock.Visibility = value ? Visibility.Visible : Visibility.Collapsed); }
		}

		// Bottom corner limit in pixels — keep in sync with BrowserWindow.BottomCornerLimitHeight
		// Change this if you adjust the reserved bottom area in BrowserWindow (e.g. for taskbar)
		private const double BottomCornerLimitHeight = 0; // <-- changeable value: bottom corner limit height in px

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID_CTRL_L)
			{
				// Toggle state and show message box
				isCtrlLToggleOn = !isCtrlLToggleOn;
				var text = isCtrlLToggleOn ? "ON" : "OFF";
				InitializeBounds();
				if (text == "ON")
				{
					blackWindow.Hide();
					ShowWindowsTaskbar();
					enableRuntimeWindow();
					UnhookStartButton();
					EnableStart();
					notificationPopup.ShowNotification("Debug Enabled!", NotificationType.Info, false);
				}
				else
				{
					blackWindow.Show();
					KeySimulator.AltTab();
					disableRuntimeWindow();
					HideWindowsTaskbar();
					HookStartButton();
					DisableStart();
					notificationPopup.ShowNotification("Debug Disabled", NotificationType.Info, false);
				}
				ApplyCtrlLToggleState();
				handled = true;
			}
			InitializeBounds();
			return IntPtr.Zero;
		}

		private IntPtr LowLevelKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
            if (nCode >= 0 && (wParam.ToInt32() == WM_KEYDOWN || wParam.ToInt32() == WM_SYSKEYDOWN))
			{
				var vkCode = Marshal.ReadInt32(lParam);
				var flags = Marshal.ReadInt32(lParam, 8);

				if (isCtrlLToggleOn && vkCode == VK_TAB && (flags & 0x20) != 0)
				{
					try
					{
						ShowTaskSwitcher();
					}
					catch { }
					return (IntPtr)1;
               }
				if (suppressWinKey && (vkCode == VK_LWIN || vkCode == VK_RWIN))
				{
					return (IntPtr)1;
				}
			}

			return CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
		}

		private bool suppressWinKey = false;

		public void HookStartButton()
		{
			suppressWinKey = true;
			try { EnableKeyboardHook(); } catch { }
		}

		public void UnhookStartButton()
		{
         // Clear suppression flag and optionally remove the hook if it's
			// no longer required.
			suppressWinKey = false;
			try { DisableKeyboardHook(); } catch { }
		}

     #if !DISABLE_PRINTSCREEN_HIDE
		private IntPtr PrintScreenHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0 && (wParam.ToInt32() == WM_KEYDOWN || wParam.ToInt32() == WM_SYSKEYDOWN))
			{
				var vkCode = Marshal.ReadInt32(lParam);
				if (vkCode == VK_SNAPSHOT)
				{
					// Hide our taskbar briefly so screenshots don't include it
					try
					{
						Dispatcher.BeginInvoke((Action)(() =>
						{
							ShowWindowsTaskbar();
							this.Hide();
						}));
						System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
						{
							Dispatcher.BeginInvoke((Action)(() =>
							{
								this.Show();
								HideWindowsTaskbar();
							}));
						});
					}
					catch { }
				}
			}

			return CallNextHookEx(printScreenHookHandle, nCode, wParam, lParam);
		}
		#endif

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

		private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
		private const uint KEYEVENTF_KEYUP = 0x0002;

		private void ShowTaskSwitcher()
		{
			const byte VK_TAB_B = 0x09; // Tab

			// Tab down
			keybd_event(VK_TAB_B, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
			// Tab up
			keybd_event(VK_TAB_B, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("shell32.dll")]
		private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

		private const uint ABM_GETSTATE = 0x00000004;

		[StructLayout(LayoutKind.Sequential)]
		private struct APPBARDATA
		{
			public int cbSize;
			public IntPtr hWnd;
			public uint uCallbackMessage;
			public uint uEdge;
			public RECT rc;
			public IntPtr lParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		private const uint ABM_SETSTATE = 0x0000000A;
		private const uint ABS_AUTOHIDE = 0x1;
		private const uint ABS_ALWAYSONTOP = 0x2;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int vKey);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		private const int SW_HIDE = 0;
		private const int SW_SHOW = 5;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SystemParametersInfo(int uiAction, int uiParam, string pvParam, int fWinIni);

		private const int SPI_SETDESKWALLPAPER = 0x0014;
		private const int SPIF_UPDATEINIFILE = 0x01;
		private const int SPIF_SENDCHANGE = 0x02;

		private void ApplyCtrlLToggleState()
		{
			try
			{
				if (isCtrlLToggleOn)
				{
					//DisableMultiFingerGestures();
					DisableStart();
					WinKeyBlocker.DisableBlock();
					EnableKeyboardHook();

				}
				else
				{
					WinKeyBlocker.EnableBlock();
					EnableStart();
					//EnableMultiFingerGestures();
                   DisableKeyboardHook();
				}
			}
			catch (Exception ex)
			{
				logger.Error("Failed to apply Ctrl+L toggle state.", ex);
			}
		}
		private void EnableKeyboardHook()
		{
			try
			{
				if (keyboardHookHandle == IntPtr.Zero)
				{
					if (keyboardProcDelegate == null)
					{
						keyboardProcDelegate = LowLevelKeyboardHookCallback;
					}
					var moduleHandle = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
					keyboardHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProcDelegate, moduleHandle, 0);
					if (keyboardHookHandle == IntPtr.Zero)
					{
						logger.Debug("Failed to set low-level keyboard hook.");
					}
				}

			}
			catch (Exception ex)
			{
				logger.Error("Exception while enabling keyboard hook.", ex);
			}
		}

		private void DisableKeyboardHook()
		{
			try
			{
				if (keyboardHookHandle != IntPtr.Zero)
				{
					UnhookWindowsHookEx(keyboardHookHandle);
					keyboardHookHandle = IntPtr.Zero;
				}
			}
			catch (Exception ex)
			{
				logger.Error("Exception while disabling keyboard hook.", ex);
           }
		}


		void enableRuntimeWindow()
		{
			try
			{
				string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string targetFolder = Path.Combine(roaming, "SafeExamBrowser\\p_cfg");
				Directory.CreateDirectory(targetFolder);
				string fileName = "logging";
				string filePath = Path.Combine(targetFolder, fileName);

				if (!File.Exists(filePath))
				{
					using (FileStream fs = File.Create(filePath))
					{
						byte[] info = System.Text.Encoding.UTF8.GetBytes("Log created: " + DateTime.Now.ToString("o") + Environment.NewLine);
						fs.Write(info, 0, info.Length);
					}
					Console.WriteLine($"File created: {filePath}");
				}
				else
				{
					string line = "App run: " + DateTime.Now.ToString("o") + Environment.NewLine;
					File.AppendAllText(filePath, line);
					Console.WriteLine($"File exists. Appended: {filePath}");
				}
			}
			catch (UnauthorizedAccessException uae)
			{
				Console.WriteLine("Access denied: " + uae.Message);
			}
			catch (IOException ioe)
			{
				Console.WriteLine("IO error: " + ioe.Message);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
			}
		}
		void disableRuntimeWindow()
		{
			try
			{
				string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

				string filePath = Path.Combine(roaming, "SafeExamBrowser\\p_cfg", "logging");

				if (File.Exists(filePath))
				{
					File.Delete(filePath);
					Console.WriteLine("File 'logging' berhasil dihapus.");
				}
				else
				{
					Console.WriteLine("File tidak ditemukan, tidak ada yang dihapus.");
				}
			}
			catch (IOException ioe)
			{
				Console.WriteLine("Gagal menghapus! File sedang digunakan: " + ioe.Message);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Terjadi kesalahan: " + ex.Message);
			}
		}
		void checkProctoringState()
		{
			try
			{
				string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string filePath = Path.Combine(roaming, "SafeExamBrowser\\p_cfg", "proctoring");

				if (File.Exists(filePath))
				{

					using (StreamReader reader = new StreamReader(filePath))
					{
						string line;
						while ((line = reader.ReadLine()) != null)
						{
							if (line == "active")
								SetDummyProctor(DummyProctorState.Active);
							else
								SetDummyProctor(DummyProctorState.Inactive);
						}
					}
				}
				else
				{
					SetDummyProctor(DummyProctorState.None);
				}
			}
			catch (IOException ioe)
			{
				Console.WriteLine("Gagal menghapus! File sedang digunakan: " + ioe.Message);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Terjadi kesalahan: " + ex.Message);
			}
		}
		private void HideWindowsTaskbar()
		{
			try
			{
				SetWindowsTaskbarAutoHide(true);
			}
			catch (Exception ex)
			{
				logger.Error("Failed to enable automatic hiding for the Windows taskbar.", ex);
			}
		}

		private void ShowWindowsTaskbar()
		{
			try
			{
				SetWindowsTaskbarAutoHide(false);
			}
			catch (Exception ex)
			{
				logger.Error("Failed to disable automatic hiding for the Windows taskbar.", ex);
			}
		}

		private static void SetWindowsTaskbarAutoHide(bool enabled)
		{
			var appBarData = new APPBARDATA { cbSize = Marshal.SizeOf(typeof(APPBARDATA)) };
			var currentState = unchecked((uint) SHAppBarMessage(ABM_GETSTATE, ref appBarData).ToInt64());
			var newState = enabled ? currentState | ABS_AUTOHIDE : currentState & ~ABS_AUTOHIDE;

			if (newState == currentState)
			{
				return;
			}

			appBarData.lParam = (IntPtr) (long) newState;
			SHAppBarMessage(ABM_SETSTATE, ref appBarData);
		}

		public void DisableStart()
		{
			try
			{
				// StartMenuExperienceHost keeps its package directory open, so it must be
				// stopped before the directory can be renamed.
				var processes = Process.GetProcessesByName("StartMenuExperienceHost");
				foreach (var proc in processes)
				{
					using (proc)
					{
						proc.Kill();
						proc.WaitForExit();
					}
				}

				var sourcePath = Path.Combine(BasePath, OriginalName);
				var disabledPath = Path.Combine(BasePath, DisabledName);

				if (Directory.Exists(sourcePath))
				{
					Directory.Move(sourcePath, disabledPath);
				}

				SetStartMenuContextMenu(@"c:\windows\SystemResources\compstui.dll.mun,-64007", "Enable Start-menu");
				logger.Info("Disabled the Windows Start menu.");
			}
			catch (Exception ex)
			{
				logger.Error("Failed to disable the Windows Start menu.", ex);
			}
		}

		public void EnableStart()
		{
			try
			{
				var disabledPath = Path.Combine(BasePath, DisabledName);
				var sourcePath = Path.Combine(BasePath, OriginalName);

				if (Directory.Exists(disabledPath))
				{
					Directory.Move(disabledPath, sourcePath);
				}

				SetStartMenuContextMenu(@"c:\windows\SystemResources\compstui.dll.mun,-64008", "Disable Start-menu");
				logger.Info("Enabled the Windows Start menu.");
			}
			catch (Exception ex)
			{
				logger.Error("Failed to enable the Windows Start menu.", ex);
			}
		}

		private static void SetStartMenuContextMenu(string icon, string verb)
		{
			using (var key = Registry.ClassesRoot.CreateSubKey(StartMenuContextMenuKey))
			{
				if (key == null)
				{
					throw new InvalidOperationException("Unable to open the Start menu context-menu registry key.");
				}

				key.SetValue("icon", icon, RegistryValueKind.String);
				key.SetValue("MUIVerb", verb, RegistryValueKind.String);
			}
		}

		public void SetDummyProctor(DummyProctorState state)
		{
			// Sembunyikan keduanya terlebih dahulu sebagai default
			proctorOnIndicator.Visibility = Visibility.Collapsed;
			proctorOffIndicator.Visibility = Visibility.Collapsed;

			switch (state)
			{
				case DummyProctorState.Active:
					proctorOnIndicator.Visibility = Visibility.Visible;
					break;

				case DummyProctorState.Inactive:
					proctorOffIndicator.Visibility = Visibility.Visible;
					break;

				case DummyProctorState.None:
					proctorOffIndicator.Visibility = Visibility.Collapsed;
					proctorOnIndicator.Visibility = Visibility.Collapsed;
					// Keduanya tetap Collapsed
					break;
			}
		}

		public bool ShowQuitButton
		{
			set { Dispatcher.Invoke(() => QuitButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed); }
		}

		public event LoseFocusRequestedEventHandler LoseFocusRequested;
		public event QuitButtonClickedEventHandler QuitButtonClicked;

		internal Taskbar(ILogger logger)
		{
			this.logger = logger;
			
			InitializeComponent();
			// Prepare the black overlay window so Window_Loaded can show it without NRE
			try
			{
				blackWindow = new BlackWindow();
				blackWindow.ShowInTaskbar = false;
			}
			catch { }

			InitializeTaskbar();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var helper = new WindowInteropHelper(this);
			hwndSource = HwndSource.FromHwnd(helper.Handle);
			hwndSource.AddHook(WndProc);

			try
			{
				hotkeyRegistered = RegisterHotKey(helper.Handle, HOTKEY_ID_CTRL_L, MOD_CONTROL, VK_L);
				if (!hotkeyRegistered)
				{
					logger.Debug("Failed to register global hotkey Ctrl+L.");
				}
			}
			catch (Exception ex)
			{
				logger.Error("Exception while registering global hotkey.", ex);
			}

            // Always register a low-level hook to detect PrintScreen so we can hide our taskbar briefly while screenshots are taken.
			#if !DISABLE_PRINTSCREEN_HIDE
			try
			{
				printScreenProcDelegate = PrintScreenHookCallback;
				var moduleHandle = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
				printScreenHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, printScreenProcDelegate, moduleHandle, 0);
				if (printScreenHookHandle == IntPtr.Zero)
				{
					logger.Debug("Failed to set PrintScreen low-level keyboard hook.");
				}
			}
			catch (Exception ex)
			{
				logger.Error("Exception while setting PrintScreen keyboard hook.", ex);
			}
			#endif
		}

		public void AddApplicationControl(IApplicationControl control, bool atFirstPosition = false)
		{
			if (control is UIElement uiElement)
			{
				if (atFirstPosition)
				{
					ApplicationStackPanel.Children.Insert(0, uiElement);
				}
				else
				{
					ApplicationStackPanel.Children.Add(uiElement);
				}
			}
		}

		public void AddNotificationControl(INotificationControl control)
		{
			if (control is UIElement uiElement)
			{
				NotificationStackPanel.Children.Add(uiElement);
			}
		}

		public void AddSystemControl(ISystemControl control)
		{
			if (control is UIElement uiElement)
			{
				SystemControlStackPanel.Children.Add(uiElement);
			}
		}

		public new void Close()
		{
			Dispatcher.Invoke(base.Close);
		}

		public void Focus(bool forward)
		{
			Dispatcher.BeginInvoke((Action) (() =>
			{
				Activate();

				if (forward && ApplicationStackPanel.Children.Count > 0)
				{
					SetFocusWithin(ApplicationStackPanel.Children[0]);
				}
				else
				{
					QuitButton.Focus();
				}
			}));
		}

		public int GetAbsoluteHeight()
		{
			return Dispatcher.Invoke(() =>
			{
				var height = (int) this.TransformToPhysical(Width, Height).Y;

				logger.Debug($"Calculated physical taskbar height is {height}px.");

				return height;
			});
		}

		public int GetRelativeHeight()
		{
			return Dispatcher.Invoke(() =>
			{
				var height = (int) Height;

				logger.Debug($"Logical taskbar height is {height}px.");

				return height;
			});
		}
		public void InitializeBounds()
		{
			Dispatcher.Invoke(() =>
			{
                Width = SystemParameters.PrimaryScreenWidth;
				Left = 0;
				// Position the taskbar so it sits inside the bottom corner limit reserved by BrowserWindow
				Top = SystemParameters.PrimaryScreenHeight - BottomCornerLimitHeight - Height
				;

				var position = this.TransformToPhysical(Left, Top);
				var size = this.TransformToPhysical(Width, Height);

				logger.Debug($"Set taskbar bounds to {Width}x{Height} at ({Left}/{Top}), in physical pixels: {size.X}x{size.Y} at ({position.X}/{position.Y}).");
			});
		}

		private void InitializeTaskbar()
		{
			Closing += Taskbar_Closing;
			Loaded += (o, args) => InitializeBounds();
			QuitButton.Clicked += QuitButton_Clicked;
		}

		public void InitializeText(IText text)
		{
			Dispatcher.Invoke(() =>
			{
				var txt = text.Get(TextKey.Shell_QuitButton);
				QuitButton.ToolTip = txt;
				QuitButton.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, txt);
			});
		}

		public void Register(ITaskbarActivator activator)
		{
			activator.Activated += Activator_Activated;
		}

		public new void Show()
		{
			Dispatcher.Invoke(base.Show);
		}

		private void Activator_Activated()
		{
			(this as ITaskbar).Focus(true);
		}

		private void QuitButton_Clicked(CancelEventArgs args)
		{
			QuitButtonClicked?.Invoke(args);
			allowClose = !args.Cancel;
		}

		private void Taskbar_Closing(object sender, CancelEventArgs e)
		{
         // Unregister global hotkey if registered
			try
			{
				if (hotkeyRegistered)
				{
					var helper = new System.Windows.Interop.WindowInteropHelper(this);
					UnregisterHotKey(helper.Handle, HOTKEY_ID_CTRL_L);
					enableRuntimeWindow();
					hotkeyRegistered = false;
				}
			}
			catch { }

			// Unhook low-level keyboard hook
			try
			{
				if (keyboardHookHandle != IntPtr.Zero)
				{
					UnhookWindowsHookEx(keyboardHookHandle);
					keyboardHookHandle = IntPtr.Zero;
				}
			}
			catch { }

			// Unhook PrintScreen hook
         #if !DISABLE_PRINTSCREEN_HIDE
			try
			{
				if (printScreenHookHandle != IntPtr.Zero)
				{
					UnhookWindowsHookEx(printScreenHookHandle);
					printScreenHookHandle = IntPtr.Zero;
				}
			}
			catch { }
			#endif

			// Ensure taskbar is restored when application closes
			try
			{
				ShowWindowsTaskbar();
			}
			catch { }

			if (allowClose)
			{
				foreach (var child in SystemControlStackPanel.Children)
				{
					if (child is ISystemControl systemControl)
					{
						systemControl.Close();
					}
				}
			}
			else
			{
				e.Cancel = true;
			}
		}

		private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			// Toggle and show ON/OFF when LeftCtrl + L is pressed
			if (e.Key == System.Windows.Input.Key.L && System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl))
			{
             // Only handle the toggle here if the global hotkey couldn't be registered.
				if (!hotkeyRegistered)
				{
					isCtrlLToggleOn = !isCtrlLToggleOn;
					var state = isCtrlLToggleOn ? "ON" : "OFF";
					for (int i = 0; i < 12; i++) { logger.Info(state); }
					ApplyCtrlLToggleState();
				}
				e.Handled = true;
			}
		}

		private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Tab)
			{
				var shift = System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift);
				var hasFocus = ApplicationStackPanel.Children.Count > 0 && ApplicationStackPanel.Children[0].IsKeyboardFocusWithin;

				if (!shift && hasFocus && isQuitButtonFocusedAtKeyDown)
				{
					LoseFocusRequested?.Invoke(true);
					e.Handled = true;
				}
				else if (shift && QuitButton.IsKeyboardFocusWithin && isFirstChildFocusedAtKeyDown)
				{
					LoseFocusRequested?.Invoke(false);
					e.Handled = true;
				}
			}

			isQuitButtonFocusedAtKeyDown = false;
			isFirstChildFocusedAtKeyDown = false;
		}


		private bool SetFocusWithin(UIElement uIElement)
		{
			if (uIElement.Focusable)
			{
				uIElement.Focus();

				return true;
			}

			if (uIElement is System.Windows.Controls.Panel)
			{
				var panel = uIElement as System.Windows.Controls.Panel;

				for (var i = 0; i < panel.Children.Count; i++)
				{
					if (SetFocusWithin(panel.Children[i]))
					{
						return true;
					}
				}

				return false;
			}
			else if (uIElement is System.Windows.Controls.ContentControl)
			{
				var control = uIElement as System.Windows.Controls.ContentControl;
				var content = control.Content as UIElement;

				if (content != null)
				{
					return SetFocusWithin(content);
				}
			}

			return false;
		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			blackWindow.Show();
			FocusLock.Disable();
			HideWindowsTaskbar();
			proctorOnIndicator.Visibility = Visibility.Collapsed;
			proctorOffIndicator.Visibility = Visibility.Collapsed;
			try
			{
				FocusLock.Disable();
				initDummyProctor();
				checkProctoringState();
				HideWindowsTaskbar();
				disableRuntimeWindow();
				HookStartButton();
			}
			catch { }
			disableRuntimeWindow();
		}

		private void Window_Unloaded(object sender, RoutedEventArgs e)
		{
			blackWindow.Hide();
			blackWindow.Close();
			enableRuntimeWindow();
			EnableStart();
			ShowWindowsTaskbar();
			FocusLock.Disable();
		}
		void initDummyProctor() {
			string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dir = Path.Combine(roaming, "SafeExamBrowser");
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			string filePath = Path.Combine(dir, "p_cfg\\" +  "proctoring");
			if (!File.Exists(filePath)) { File.Create(filePath).Close(); }

			try
			{
				// Open the text file using a stream reader.
				StreamReader reader = new StreamReader(filePath);

				// Read the stream as a string.
				string text = reader.ReadToEnd();
				// Write the text to the console.
				if (text == "active")
					SetDummyProctor(DummyProctorState.Active);
				else if(text == "inactive")
					SetDummyProctor(DummyProctorState.Inactive);
				else
					SetDummyProctor(DummyProctorState.None);
			}
			catch (IOException e)
			{
				Console.WriteLine("The file could not be read:");
				Console.WriteLine(e.Message);
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			notificationPopup.ShowNotification("This is a test notification!", NotificationType.Info, false);
			notificationPopup.Show();
		}
	}
	internal sealed class NativeMethods
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

		internal const int SPI_SETDESKWALLPAPER = 20;
		internal const int SPIF_UPDATEINIFILE = 0x01;
		internal const int SPIF_SENDWININICHANGE = 0x02;
		public const int HWND_BROADCAST = 0xffff;
		public const int WM_SETTINGCHANGE = 0x001A;
		[Flags]
		public enum SendMessageTimeoutFlags : uint
		{
			SMTO_NORMAL = 0x0,
			SMTO_BLOCK = 0x1,
			SMTO_ABORTIFHUNG = 0x2,
			SMTO_NOTIMEOUTIFNOTHUNG = 0x8,
			SMTO_ERRORONEXIT = 0x20
		}
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessageTimeout(
		IntPtr hWnd,
		uint Msg,
		IntPtr wParam,
		string lParam,
		SendMessageTimeoutFlags flags,
		uint timeout,
		out IntPtr lpdwResult);
	}
}
