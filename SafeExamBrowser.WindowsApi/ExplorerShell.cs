/*
 * Copyright (c) 2026 ETH Zürich, IT Services
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.WindowsApi.Contracts;
using SafeExamBrowser.WindowsApi.Types;

namespace SafeExamBrowser.WindowsApi
{
	public class ExplorerShell : IExplorerShell
	{
		private ILogger logger;
		private INativeMethods nativeMethods;
		private IList<Window> minimizedWindows = new List<Window>();

		public ExplorerShell(ILogger logger, INativeMethods nativeMethods)
		{
			this.logger = logger;
			this.nativeMethods = nativeMethods;
			this.minimizedWindows = new List<Window>();
		}

		public void HideAllWindows()
		{
			logger.Info("Searching for windows to be minimized...");

			foreach (var handle in nativeMethods.GetOpenWindows())
			{
				var window = new Window
				{
					Handle = handle,
					Title = nativeMethods.GetWindowTitle(handle)
				};

				minimizedWindows.Add(window);
				logger.Info($"Found window '{window.Title}' with handle = {window.Handle}.");
			}

			logger.Info("Minimizing all open windows...");
			nativeMethods.MinimizeAllOpenWindows();
			logger.Info("Open windows successfully minimized.");
		}

		public void RestoreAllWindows()
		{
			logger.Info("Restoring all minimized windows...");

			foreach (var window in minimizedWindows)
			{
				nativeMethods.RestoreWindow(window.Handle);
				logger.Info($"Restored window '{window.Title}' with handle = {window.Handle}.");
			}

			minimizedWindows.Clear();
			logger.Info("Minimized windows successfully restored.");
		}

		public void Start()
		{
			var process = new System.Diagnostics.Process();
			var explorerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");

			logger.Debug("Starting explorer shell process...");

			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.FileName = explorerPath;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			process.Start();

			logger.Debug("Waiting for explorer shell to initialize...");

			while (nativeMethods.GetShellWindowHandle() == IntPtr.Zero)
			{
				Thread.Sleep(20);
			}

			process.Refresh();
			logger.Info($"Explorer shell successfully started with PID = {process.Id}.");
			process.Close();
		}

		public void Terminate()
		{
            // Termination of the system explorer shell is disabled to avoid
			// forcibly ending the user's desktop environment. This method is
			// intentionally left as a no-op so calling code cannot terminate
			// or replace the explorer process.
			logger.Info("Explorer shell termination is disabled in this build. Skipping termination.");
		}

		private void KillExplorerShell(int processId)
		{
            // Forceful termination of explorer is disabled. Do nothing.
			logger.Warn("KillExplorerShell was invoked but forceful termination is disabled in this build.");
		}
	}
}
