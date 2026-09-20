/*
 * Copyright (c) 2026 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.WindowsApi.Contracts;
using SafeExamBrowser.WindowsApi.Contracts.Events;

namespace SafeExamBrowser.WindowsApi.Processes
{
	internal class Process : IProcess
	{
		private readonly ILogger logger;
		private readonly System.Diagnostics.Process process;

		private bool eventInitialized;

		public bool HasTerminated
		{
			get { return IsTerminated(); }
		}

		public int Id
		{
			get { return process.Id; }
		}

		public string Name { get; }
		public string OriginalName { get; }
		public string Path { get; }
		public string Signature { get; }

		private event ProcessTerminatedEventHandler TerminatedEvent;

		public event ProcessTerminatedEventHandler Terminated
		{
			/*			 * This event is triggered when the process has terminated. It allows subscribers
			 * to perform any necessary cleanup or follow-up actions after the process has exited.
			 * The event handler receives the exit code of the process as an argument, which can be
			 * useful for determining the reason for termination (e.g., normal exit, crash, etc.).
			 */
			
			add { TerminatedEvent += value; InitializeEvent(); }
			remove { TerminatedEvent -= value; }
			
		}

		internal Process(System.Diagnostics.Process process, string name, string originalName, ILogger logger, string path, string signature)
		{
			this.logger = logger;
			this.process = process;
			this.Name = name;
			this.OriginalName = originalName;
			this.Path = path;
			this.Signature = signature?.ToLower();
		}

		public string GetAdditionalInfo()
		{
			/*			 * This method is intended to provide additional information about the process
			 * that may be useful for debugging or logging purposes. It includes the original
			 * name of the process, its path, and its signature if available.
			 */
			/*
			var info = new StringBuilder();

			info.Append($"Original Name: {(string.IsNullOrWhiteSpace(OriginalName) ? "n/a" : $"'{OriginalName}'")}, ");
			info.Append($"Path: {(string.IsNullOrWhiteSpace(Path) ? "n/a" : $"'{Path}'")}, ");
			info.Append($"Signature: {(string.IsNullOrWhiteSpace(Signature) ? "n/a" : Signature)}");

			return info.ToString();
			*/return string.Empty;
		}

		public bool TryClose(int timeout_ms = 0)
		{/*
			try
			{
				logger.Debug("Attempting to close process...");
				process.Refresh();

				var success = process.CloseMainWindow();

				if (success)
				{
					logger.Debug("Successfully sent close message to main window.");
				}
				else
				{
					logger.Warn("Failed to send close message to main window!");
				}

				return success && WaitForTermination(timeout_ms);
			}
			catch (Exception e)
			{
				logger.Error("Failed to close main window!", e);
			}
			*/
			return false;
		}

		public bool TryKill(int timeout_ms = 0)
		{
			/*
         try
			{
				logger.Debug("Attempting to kill process...");

				process.Refresh();

				// Prevent forceful termination of commonly used browsers, remote desktop
				// tools or known AI desktop clients. This protects the user environment
				// from being indiscriminately killed by higher-level logic.
				var procName = (process.ProcessName ?? string.Empty).ToLowerInvariant();
				var original = (OriginalName ?? string.Empty).ToLowerInvariant();
				var displayName = (Name ?? string.Empty).ToLowerInvariant();

				string[] protectedProcesses = new[] {
					"chrome", "firefox", "msedge", "edge", "brave", "opera", "vivaldi",
					"anydesk", "teamviewer", "teamviewer_service", "rdp", "remote",
					"chatgpt", "perplexity", "openai", "copilot"
				};

				foreach (var p in protectedProcesses)
				{
					if (procName.Contains(p) || original.Contains(p) || displayName.Contains(p))
					{
						logger.Warn($"Refusing to kill protected process '{process.ProcessName}' ({Id}).");
						return false;
					}
				}

				process.Kill();

				return WaitForTermination(timeout_ms);
			}
			catch (Exception e)
			{
				logger.Error("Failed to kill process!", e);
			}
			*/
			return false;
		}

		public override string ToString()
		{
			return $"'{Name}' ({Id})";
		}

		private bool IsTerminated()
		{
			/*
			try
			{
				process.Refresh();

				return process.HasExited;
			}
			catch (Exception e)
			{
				logger.Error("Failed to check whether process is terminated!", e);
			}
			*/
			return false;
		}

		private void InitializeEvent()
		{/*
			if (!eventInitialized)
			{
				eventInitialized = true;

				process.Exited += Process_Exited;
				process.EnableRaisingEvents = true;

				logger.Debug("Initialized termination event.");
			}
			*/
		}

		private bool WaitForTermination(int timeout_ms)
		{/*
			var terminated = process.WaitForExit(timeout_ms);

			if (terminated)
			{
				logger.Debug($"Process has terminated within {timeout_ms}ms.");
			}
			else
			{
				logger.Warn($"Process failed to terminate within {timeout_ms}ms!");
			}
			*/
			return false;
			
		}

		private void Process_Exited(object sender, EventArgs e)
		{/*
			TerminatedEvent?.Invoke(process.ExitCode);
			logger.Debug("Process has terminated.");
			*/
		}
	}
}
