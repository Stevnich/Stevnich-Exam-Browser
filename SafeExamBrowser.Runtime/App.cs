using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using SafeExamBrowser.UserInterface.Desktop.Windows;
using SafeExamBrowser.Configuration.Contracts;

namespace SafeExamBrowser.Runtime
{
	public class App : Application
	{
		private static readonly Mutex Mutex = new Mutex(true, AppConfig.RUNTIME_MUTEX_NAME);
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

			// Basic network availability check
			if (!NetworkInterface.GetIsNetworkAvailable())
			{
				try
				{
					splash.mainSplashUI.Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
				}
				catch { }

				MessageBox.Show("No network connection detected. SEB requires a network adapter / internet connection to start.\n\nPlease connect to a network and restart the application.", "No Internet Connection", MessageBoxButton.OK, MessageBoxImage.Warning);
				splash.Close();
				base.Shutdown();
				return;
			}

			instances.BuildObjectGraph(Shutdown);
			instances.LogStartupInformation();

			Task.Run(new Action(() =>
			{
				TryStart();
				// Close splash once TryStart has been invoked (TryStart will call Shutdown if startup fails)
				Dispatcher.Invoke(() => { try { splash.Close(); } catch { } });
			}));
		}

		private void TryStart()
		{
			var success = instances.RuntimeController.TryStart();

			if (!success)
			{
				Shutdown();
			}
		}

		public new void Shutdown()
		{
			Task.Run(new Action(ShutdownInternal));
		}

		private void ShutdownInternal()
		{
			instances.RuntimeController.Terminate();
			instances.LogShutdownInformation();

			Dispatcher.Invoke(base.Shutdown);
		}
	}
}
