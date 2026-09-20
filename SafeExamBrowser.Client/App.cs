/*
 * Copyright (c) 2026 ETH Zürich, IT Services
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using SafeExamBrowser.UserInterface.Desktop.Windows;
using SafeExamBrowser.Configuration.Contracts;

namespace SafeExamBrowser.Client
{
	public class App : Application
	{
		private const int ILMCM_CHECKLAYOUTANDTIPENABLED = 0x00001;
		private const int ILMCM_LANGUAGEBAROFF = 0x00002;	

		private static readonly Mutex Mutex = new Mutex(true, AppConfig.CLIENT_MUTEX_NAME);
		private readonly CompositionRoot instances = new CompositionRoot();

		[STAThread]
		public static void Main()
		{
			try
			{
				StartApplication();
			}
         catch (Exception e)
			{
				// If the failure looks like the MAC address / network adapter detection failed, show a clearer message
				var message = e.Message ?? string.Empty;
				var innerMessage = e.InnerException?.Message ?? string.Empty;
				if (message.Contains("Sequence contains no elements") || innerMessage.Contains("Sequence contains no elements"))
				{
					MessageBox.Show("Could not detect any network adapters / MAC addresses.\n\nPlease ensure the machine has a network adapter and is connected to a network (internet) and restart the application.", "Network Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
				else
				{
					MessageBox.Show(e.Message + "\n\n" + e.StackTrace, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			finally
			{
				Mutex.Close();
			}
		}

		private static void StartApplication()
		{
			if (NoInstanceRunning())
			{
				new App().Run();
			}
			else
			{
				MessageBox.Show("You can only run one instance of SEB at a time.", "Startup Not Allowed", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private static bool NoInstanceRunning()
		{
			return Mutex.WaitOne(TimeSpan.Zero, true);
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			ShutdownMode = ShutdownMode.OnExplicitShutdown;
			// Show splash screen early so we can modify its UI for error states
			var splash = new SafeExamBrowser.UserInterface.Desktop.Windows.SplashScreen();
			splash.Show();

			// Check for basic network availability before initializing components that expect a network adapter / MAC address.
			// This avoids an unhelpful crash deep in startup when no network interfaces are present.
			if (!NetworkInterface.GetIsNetworkAvailable())
			{
				// Dim the splash background to indicate an error state
				try
				{
					splash.mainSplashUI.Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
				}
				catch
				{
					// Ignore UI update errors and continue to show the message
				}

				MessageBox.Show("No network connection detected. SEB requires a network adapter / internet connection to start.\n\nPlease connect to a network and restart the application.", "No Internet Connection", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Close splash and exit
				splash.Close();
				base.Shutdown();
				return;
			}

			// We need to manually initialize a monitor in order to prevent Windows from automatically doing so and thus rendering an input lanuage
			// switch in the bottom right corner of the desktop. This must be done before any UI element is initialized or rendered on the screen.
			InitLocalMsCtfMonitor(ILMCM_CHECKLAYOUTANDTIPENABLED | ILMCM_LANGUAGEBAROFF);

           instances.BuildObjectGraph(Shutdown);
			instances.LogStartupInformation();

			var success = instances.ClientController.TryStart();

			if (!success)
			{
				// Close splash if still open and then shutdown
				splash.Close();
				Shutdown();
			}
			else
			{
				// Startup succeeded; close the splash
				splash.Close();
			}
		}

		public new void Shutdown()
		{
			void shutdown()
			{
				instances.ClientController.Terminate();
				instances.LogShutdownInformation();

				UninitLocalMsCtfMonitor();

				base.Shutdown();
			}

			Dispatcher.InvokeAsync(shutdown);
		}

		[DllImport("MsCtfMonitor.dll", SetLastError = true)]
		private static extern IntPtr InitLocalMsCtfMonitor(int dwFlags);

		[DllImport("MsCtfMonitor.dll", SetLastError = true)]
		private static extern IntPtr UninitLocalMsCtfMonitor();
	}
}
