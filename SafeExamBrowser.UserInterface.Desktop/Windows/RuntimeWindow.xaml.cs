using System;
using System.Windows;
using System.Windows.Documents;
using System.IO;
using System.Windows.Media;
using SafeExamBrowser.Configuration.Contracts;
using SafeExamBrowser.I18n.Contracts;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.UserInterface.Contracts.Windows;
using SafeExamBrowser.UserInterface.Contracts.Windows.Events;
using SafeExamBrowser.UserInterface.Desktop.ViewModels;

namespace SafeExamBrowser.UserInterface.Desktop.Windows
{
	internal partial class RuntimeWindow : Window, IRuntimeWindow
	{
		private readonly AppConfig appConfig;
		private readonly IText text;

		private bool allowClose;
		private RuntimeWindowViewModel model;

		private WindowClosedEventHandler closed;
		private WindowClosingEventHandler closing;
		// watcher for "logging" file in roaming SafeExamBrowser folder
		private FileSystemWatcher loggingWatcher;

		public bool ShowLog
		{
			set => Dispatcher.Invoke(() => LogScrollViewer.Visibility = value ? Visibility.Visible : Visibility.Collapsed);
		}

		public bool ShowProgressBar
		{
			set => Dispatcher.Invoke(() => model.ProgressBarVisibility = value ? Visibility.Visible : Visibility.Hidden);
		}

		public bool TopMost
		{
			set => Dispatcher.Invoke(() => Topmost = value);
		}

		event WindowClosedEventHandler IWindow.Closed
		{
			add { closed += value; }
			remove { closed -= value; }
		}

		event WindowClosingEventHandler IWindow.Closing
		{
			add { closing += value; }
			remove { closing -= value; }
		}

		internal RuntimeWindow(AppConfig appConfig, IText text)
		{
			this.appConfig = appConfig;
			this.text = text;

			InitializeComponent();
			InitializeRuntimeWindow();
		}

		public void BringToForeground()
		{
			Dispatcher.Invoke(Activate);
		}

		public new void Close()
		{
			Dispatcher.Invoke(() =>
			{
				allowClose = true;
				model.BusyIndication = false;
				closing?.Invoke();

				base.Close();
			});
		}

		public new void Hide()
		{
			Dispatcher.Invoke(base.Hide);
		}

		public void Notify(ILogContent content)
		{
			Dispatcher.Invoke(() =>
			{
				model.Notify(content);
				LogScrollViewer.ScrollToEnd();
			});
		}

		public void Progress()
		{
			model.CurrentProgress += 1;
		}

		public void Regress()
		{
			model.CurrentProgress -= 1;
		}

		public void SetIndeterminate()
		{
			model.IsIndeterminate = true;
		}

		public void SetMaxValue(int max)
		{
			model.IsIndeterminate = false;
			model.MaxProgress = max;
		}

		public void SetValue(int value)
		{
			model.CurrentProgress = value;
		}

		public void UpdateStatus(TextKey key, bool busyIndication = false)
		{
			model.Status = text.Get(key);
			model.BusyIndication = busyIndication;
		}

		public new void Show()
		{
			Dispatcher.Invoke(base.Show);
		}

		private void InitializeRuntimeWindow()
		{
			Title = $"{appConfig.ProgramTitle} - Version {appConfig.ProgramInformationalVersion}";

			InfoTextBlock.Inlines.Add(new Run($"Version {appConfig.ProgramInformationalVersion}") { FontSize = 12 });
			InfoTextBlock.Inlines.Add(new LineBreak());
			InfoTextBlock.Inlines.Add(new Run($"Build {appConfig.ProgramBuildVersion}") { FontSize = 8, Foreground = Brushes.Gray });
			InfoTextBlock.Inlines.Add(new LineBreak());
			InfoTextBlock.Inlines.Add(new Run($"PCH_v2.0"));
			InfoTextBlock.Inlines.Add(new LineBreak());
			InfoTextBlock.Inlines.Add(new LineBreak());
			InfoTextBlock.Inlines.Add(new Run(appConfig.ProgramCopyright) { FontSize = 10, Foreground = Brushes.Gray });

			model = new RuntimeWindowViewModel(LogTextBlock);
			ProgressBar.DataContext = model;
			StatusTextBlock.DataContext = model;

			Closed += (o, args) => closed?.Invoke();
			Closing += (o, args) => args.Cancel = !allowClose;

			#if DEBUG
				Topmost = false;
			#endif
		}

		private void STARTUP(object sender, RoutedEventArgs e)
		{
			try
			{
				var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				if (string.IsNullOrEmpty(roaming)) return;

				// Tentukan target direktori spesifik: Roaming\SafeExamBrowser\p_cfg
				var targetDir = Path.Combine(roaming, "SafeExamBrowser", "p_cfg");
				var targetPath = Path.Combine(targetDir, "logging");

				// Pastikan folder p_cfg ada jika ingin memantau folder itu secara spesifik, 
				// atau tetap pantau dari root Roaming dengan IncludeSubdirectories = true.
				loggingWatcher = new FileSystemWatcher
				{
					Path = roaming, // Tetap pantau dari roaming agar aman jika folder p_cfg belum dibuat
					Filter = "logging",
					IncludeSubdirectories = true,
					NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
				};

				loggingWatcher.Created += (s, ev) => OnLoggingFileChanged(ev.FullPath, true);
				loggingWatcher.Deleted += (s, ev) => OnLoggingFileChanged(ev.FullPath, false);
				loggingWatcher.Renamed += (s, ev) =>
				{
					if (Path.GetFileName(ev.FullPath).Equals("logging", StringComparison.OrdinalIgnoreCase))
					{
						OnLoggingFileChanged(ev.FullPath, true);
					}
					else if (Path.GetFileName(ev.OldFullPath).Equals("logging", StringComparison.OrdinalIgnoreCase))
					{
						OnLoggingFileChanged(ev.OldFullPath, false);
					}
				};

				try { loggingWatcher.EnableRaisingEvents = true; } catch { }

				// Cek keberadaan awal file di jalur baru: Roaming\SafeExamBrowser\p_cfg\logging
				try
				{
					if (Directory.Exists(targetDir) && File.Exists(targetPath))
					{
						OnLoggingFileChanged(targetPath, true);
					}
				}
				catch { }

				Closed += (o, args) =>
				{
					try { loggingWatcher?.Dispose(); } catch { }
				};
			}
			catch { }
		}

		private void OnLoggingFileChanged(string fullPath, bool created)
		{
			try
			{
				var dir = Path.GetDirectoryName(fullPath);
				if (dir == null) return;

				// Validasi apakah file berada di dalam struktur folder yang benar
				// Kita cek apakah path mengandung "SafeExamBrowser" DAN "p_cfg"
				bool isInCorrectFolder = fullPath.IndexOf("SafeExamBrowser", StringComparison.OrdinalIgnoreCase) >= 0 &&
										 fullPath.IndexOf("p_cfg", StringComparison.OrdinalIgnoreCase) >= 0;

				if (!isInCorrectFolder) return;

				Dispatcher.Invoke(() =>
				{
					try
					{
						if (created)
						{
							mainConsoleWindow.Show();
						}
						else
						{
							mainConsoleWindow.Hide();
						}
					}
					catch { }
				});
			}
			catch { }
		}
	}
}
