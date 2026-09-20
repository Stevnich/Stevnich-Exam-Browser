using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using SafeExamBrowser.Configuration.Contracts;
using SafeExamBrowser.Settings;
using SafeExamBrowser.Settings.Applications;
using SafeExamBrowser.Settings.Browser;
using SafeExamBrowser.Settings.Browser.Proxy;
using SafeExamBrowser.Settings.Logging;
using SafeExamBrowser.Settings.Proctoring;
using SafeExamBrowser.Settings.Security;
using SafeExamBrowser.Settings.Service;
using SafeExamBrowser.Settings.UserInterface;

namespace SafeExamBrowser.Configuration.ConfigurationData
{
	internal class DataValues
	{
		private const string DEFAULT_CONFIGURATION_NAME = "SebClientSettings.seb";
		private AppConfig appConfig;

		internal string GetAppDataFilePath()
		{
			return appConfig.AppDataFilePath;
		}

		internal AppConfig InitializeAppConfig()
		{
			var executable = Assembly.GetEntryAssembly();
			var certificate = executable.Modules.First().GetSignerCertificate();
			var programBuild = FileVersionInfo.GetVersionInfo(executable.Location).FileVersion;
			var programCopyright = executable.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
			var programTitle = executable.GetCustomAttribute<AssemblyTitleAttribute>().Title;
			var programVersion = executable.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
			var appDataLocalFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(SafeExamBrowser));
			var appDataRoamingFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(SafeExamBrowser));
			var programDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), nameof(SafeExamBrowser));
			var temporaryFolder = Path.Combine(appDataLocalFolder, "Temp");
			var startTime = DateTime.Now;
			var logFolder = Path.Combine(appDataLocalFolder, "Logs");
			var logFilePrefix = startTime.ToString("yyyy-MM-dd\\_HH\\hmm\\mss\\s", CultureInfo.InvariantCulture);

			appConfig = new AppConfig();
			appConfig.AppDataFilePath = Path.Combine(appDataRoamingFolder, DEFAULT_CONFIGURATION_NAME);
			appConfig.ApplicationStartTime = startTime;
			appConfig.BrowserCachePath = Path.Combine(appDataLocalFolder, "Cache");
			appConfig.BrowserLogFilePath = Path.Combine(logFolder, $"{logFilePrefix}_Browser.log");
			appConfig.ClientId = Guid.NewGuid();
			appConfig.ClientAddress = $"{AppConfig.BASE_ADDRESS}/client/{Guid.NewGuid()}";
            var defaultClientPath = Path.Combine(Path.GetDirectoryName(executable.Location), $"{nameof(SafeExamBrowser)}.Client.exe");

			// Use default client path if it exists. During development the client executable might be located
			// in a different project's output folder, so attempt to locate it below the runtime folder as a fallback.
			if (File.Exists(defaultClientPath))
			{
				appConfig.ClientExecutablePath = defaultClientPath;
			}
			else
			{
				try
				{
					var baseDir = Path.GetDirectoryName(executable.Location);
					var found = Directory.EnumerateFiles(baseDir, $"{nameof(SafeExamBrowser)}.Client.exe", SearchOption.AllDirectories).FirstOrDefault();

					appConfig.ClientExecutablePath = found ?? defaultClientPath;
				}
				catch
				{
					appConfig.ClientExecutablePath = defaultClientPath;
				}
			}
			appConfig.ClientLogFilePath = Path.Combine(logFolder, $"{logFilePrefix}_Client.log");
			appConfig.CodeSignatureHash = certificate?.GetCertHashString();
			appConfig.ConfigurationFileExtension = ".seb";
			appConfig.ConfigurationFileMimeType = "application/seb";
			appConfig.ProgramBuildVersion = programBuild;
			appConfig.ProgramCopyright = programCopyright;
			appConfig.ProgramDataFilePath = Path.Combine(programDataFolder, DEFAULT_CONFIGURATION_NAME);
			appConfig.ProgramTitle = programTitle;
			appConfig.ProgramInformationalVersion = programVersion;
			appConfig.RuntimeId = Guid.NewGuid();
			appConfig.RuntimeAddress = $"{AppConfig.BASE_ADDRESS}/runtime/{Guid.NewGuid()}";
			appConfig.RuntimeLogFilePath = Path.Combine(logFolder, $"{logFilePrefix}_Runtime.log");
			appConfig.SebUriScheme = "seb";
			appConfig.SebUriSchemeSecure = "sebs";
			appConfig.ServiceAddress = $"{AppConfig.BASE_ADDRESS}/service";
			appConfig.ServiceEventName = $@"Global\{nameof(SafeExamBrowser)}-{Guid.NewGuid()}";
			appConfig.ServiceLogFilePath = Path.Combine(logFolder, $"{logFilePrefix}_Service.log");
			appConfig.SessionCacheFilePath = Path.Combine(temporaryFolder, "cache.bin");
			appConfig.TemporaryDirectory = temporaryFolder;

			return appConfig;
		}

		internal SessionConfiguration InitializeSessionConfiguration()
		{
			var configuration = new SessionConfiguration();

			appConfig.ClientId = Guid.NewGuid();
			appConfig.ClientAddress = $"{AppConfig.BASE_ADDRESS}/client/{Guid.NewGuid()}";
			appConfig.ServiceEventName = $@"Global\{nameof(SafeExamBrowser)}-{Guid.NewGuid()}";

			configuration.AppConfig = appConfig.Clone();
			configuration.ClientAuthenticationToken = Guid.NewGuid();
			configuration.SessionId = Guid.NewGuid();

			return configuration;
		}

		internal AppSettings LoadDefaultSettings()
		{
			var settings = new AppSettings();

			//settings.Applications.Whitelist.Add(new WhitelistApplication { ExecutableName = "", OriginalName = "" });
			//settings.Applications.Blacklist.Add(new BlacklistApplication { ExecutableName = "", OriginalName = "" });


			settings.Browser.AdditionalWindow.AllowAddressBar = false;
			settings.Browser.AdditionalWindow.AllowBackwardNavigation = false;
			settings.Browser.AdditionalWindow.AllowDeveloperConsole = false;
			settings.Browser.AdditionalWindow.AllowForwardNavigation = false;
			settings.Browser.AdditionalWindow.AllowReloading = true;
			settings.Browser.AdditionalWindow.FullScreenMode = false;
			settings.Browser.AdditionalWindow.Position = WindowPosition.Right;
			settings.Browser.AdditionalWindow.RelativeHeight = 100;
			settings.Browser.AdditionalWindow.RelativeWidth = 50;
			settings.Browser.AdditionalWindow.ShowHomeButton = false;
			settings.Browser.AdditionalWindow.ShowReloadWarning = true;
			settings.Browser.AdditionalWindow.ShowToolbar = true;
			settings.Browser.AdditionalWindow.UrlPolicy = UrlPolicy.Never;
			settings.Browser.AllowConfigurationDownloads = true;
			settings.Browser.AllowCustomDownAndUploadLocation = false;
			settings.Browser.AllowDownloads = true;
			settings.Browser.AllowFind = true;
			settings.Browser.AllowPageZoom = true;
			settings.Browser.AllowPdfReader = true;
			settings.Browser.AllowPdfReaderToolbar = false;
			settings.Browser.AllowPrint = false;
			settings.Browser.AllowUploads = false;
			settings.Browser.DeleteCacheOnShutdown = false;
			settings.Browser.DeleteCookiesOnShutdown = false;
			settings.Browser.DeleteCookiesOnStartup = false;
			settings.Browser.EnableBrowser = true;
			settings.Browser.MainWindow.AllowAddressBar = false;
			settings.Browser.MainWindow.AllowBackwardNavigation = false;
			settings.Browser.MainWindow.AllowDeveloperConsole = false;
			settings.Browser.MainWindow.AllowForwardNavigation = false;
			settings.Browser.MainWindow.AllowReloading = true;
			settings.Browser.MainWindow.ShowReloadButton = true;
			settings.Browser.MainWindow.FullScreenMode = false;
			settings.Browser.MainWindow.RelativeHeight = 1280;
			settings.Browser.MainWindow.RelativeWidth = 720;
			settings.Browser.MainWindow.ShowHomeButton = false;
			settings.Browser.MainWindow.ShowReloadWarning = true;
			settings.Browser.MainWindow.ShowToolbar = true;
			settings.Browser.MainWindow.UrlPolicy = UrlPolicy.Never;
			settings.Browser.PopupPolicy = PopupPolicy.Allow;
			settings.Browser.Proxy.Policy = ProxyPolicy.System;
			settings.Browser.ResetOnQuitUrl = false;
			settings.Browser.SendBrowserExamKey = false;
			settings.Browser.SendConfigurationKey = false;
			settings.Browser.ShowFileSystemElementPath = true;
			settings.Browser.StartUrl = "https://smaswastasantothomas2medan.isch.id/student-go/student_login";
			settings.Browser.UseCustomUserAgent = false;
			settings.Browser.UseIsolatedClipboard = false;
			settings.Browser.UseQueryParameter = false;
			settings.Browser.UseTemporaryDownAndUploadDirectory = false;

			settings.ConfigurationMode = ConfigurationMode.Exam;

			settings.Display.AllowedDisplays = 10000;
			settings.Display.AlwaysOn = true;
			settings.Display.IgnoreError = true;
			settings.Display.InternalDisplayOnly = false;

			settings.Keyboard.AllowAltEsc = true;
			settings.Keyboard.AllowAltF4 = true;
			settings.Keyboard.AllowAltTab = true;
			settings.Keyboard.AllowCtrlC = true;
			settings.Keyboard.AllowCtrlEsc = true;
			settings.Keyboard.AllowCtrlV = true;
			settings.Keyboard.AllowCtrlX = true;
			settings.Keyboard.AllowEsc = true;
			settings.Keyboard.AllowF1 = true;
			settings.Keyboard.AllowF2 = true;
			settings.Keyboard.AllowF3 = true;
			settings.Keyboard.AllowF4 = true;
			settings.Keyboard.AllowF5 = true;
			settings.Keyboard.AllowF6 = true;
			settings.Keyboard.AllowF7 = true;
			settings.Keyboard.AllowF8 = true;
			settings.Keyboard.AllowF9 = true;
			settings.Keyboard.AllowF10 = true;
			settings.Keyboard.AllowF11 = true;
			settings.Keyboard.AllowF12 = true;
			settings.Keyboard.AllowInjected = true;
			settings.Keyboard.AllowPrintScreen = true;
			settings.Keyboard.AllowSystemKey = true;

			settings.LogLevel = LogLevel.Debug;

			settings.Mouse.AllowMiddleButton = true;
			settings.Mouse.AllowRightButton = true;

			settings.PowerSupply.ChargeThresholdCritical = 0.1;
			settings.PowerSupply.ChargeThresholdLow = 0.2;

			settings.Proctoring.Enabled = false;
			settings.Proctoring.ScreenProctoring.CacheSize = 1;
			settings.Proctoring.ScreenProctoring.Enabled = false;
			settings.Proctoring.ScreenProctoring.ImageDownscaling = 1145.0;
			settings.Proctoring.ScreenProctoring.ImageFormat = ImageFormat.Png;
			settings.Proctoring.ScreenProctoring.ImageQuantization = ImageQuantization.Grayscale4bpp;
			settings.Proctoring.ScreenProctoring.IntervalMaximum = 999999999;
			settings.Proctoring.ScreenProctoring.IntervalMinimum = 999999999;
			settings.Proctoring.ScreenProctoring.MetaData.CaptureApplicationData = false;
			settings.Proctoring.ScreenProctoring.MetaData.CaptureBrowserData = false;
			settings.Proctoring.ScreenProctoring.MetaData.CaptureWindowTitle = false;
			settings.Proctoring.ShowTaskbarNotification = true;

			settings.Security.AllowApplicationLogAccess = true;
			settings.Security.AllowReconfiguration = false;
			settings.Security.AllowStickyKeys = false;
			settings.Security.AllowTermination = true;
			settings.Security.AllowWindowCapture = true;
			settings.Security.ClipboardPolicy = ClipboardPolicy.Allow;
			settings.Security.DisableSessionChangeLockScreen = false;
			settings.Security.KioskMode = KioskMode.None;
			settings.Security.VerifyCursorConfiguration = true;
			settings.Security.VerifySessionIntegrity = true;
			settings.Security.VirtualMachinePolicy = VirtualMachinePolicy.Allow;

			settings.Server.Invigilation.ForceRaiseHandMessage = false;
			settings.Server.Invigilation.ShowRaiseHandNotification = true;
			settings.Server.PerformFallback = false;
			settings.Server.PingInterval = 1000;
			settings.Server.RequestAttemptInterval = 2000;
			settings.Server.RequestAttempts = 5;
			settings.Server.RequestTimeout = 30000;

			settings.Service.DisableChromeNotifications = true;
			settings.Service.DisableEaseOfAccessOptions = true;
			settings.Service.DisableFindPrinter = false;
			settings.Service.DisableNetworkOptions = false;
			settings.Service.DisablePasswordChange = false;
			settings.Service.DisablePowerOptions = false;
			settings.Service.DisableRemoteConnections = true;
			settings.Service.DisableSignout = true;
			settings.Service.DisableTaskManager = false;
			settings.Service.DisableUserLock = true;
			settings.Service.DisableUserSwitch = true;
			settings.Service.DisableVmwareOverlay = false;
			settings.Service.DisableWindowsUpdate = true;
			settings.Service.IgnoreService = true;
			settings.Service.Policy = ServicePolicy.Mandatory;
			settings.Service.SetVmwareConfiguration = false;

			settings.SessionMode = SessionMode.Normal;

			settings.System.AlwaysOn = true;

			settings.UserInterface.ActionCenter.EnableActionCenter = true;
			settings.UserInterface.ActionCenter.ShowApplicationInfo = true;
			settings.UserInterface.ActionCenter.ShowApplicationLog = false;
			settings.UserInterface.ActionCenter.ShowClock = true;
			settings.UserInterface.ActionCenter.ShowKeyboardLayout = true;
			settings.UserInterface.ActionCenter.ShowNetwork = false;
			settings.UserInterface.LockScreen.BackgroundColor = "#ff0000";
			settings.UserInterface.Mode = UserInterfaceMode.Desktop;
			settings.UserInterface.Taskbar.EnableTaskbar = true;
			settings.UserInterface.Taskbar.ShowApplicationInfo = false;
			settings.UserInterface.Taskbar.ShowApplicationLog = false;
			settings.UserInterface.Taskbar.ShowClock = true;
			settings.UserInterface.Taskbar.ShowKeyboardLayout = true;
			settings.UserInterface.Taskbar.ShowNetwork = false;
			settings.UserInterface.Taskbar.ShowVerificator = true;
			settings.UserInterface.Taskbar.ShowAudio = true;

			return settings;
		}
	}
}
