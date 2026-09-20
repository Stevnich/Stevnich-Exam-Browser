/*
 * Copyright (c) 2026 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.Settings.Security;
using SafeExamBrowser.WindowsApi.Contracts;


namespace SafeExamBrowser.Runtime.Session
{
	/// <summary>
	/// Manages enabling and disabling kiosk modes at runtime.
	/// </summary>
	public class KioskManager
	{
		private readonly ILogger logger;
		private readonly IDesktopFactory desktopFactory;
		private readonly IDesktopMonitor desktopMonitor;
		private readonly IExplorerShell explorerShell;
		private readonly IProcessFactory processFactory;

		private KioskMode? activeMode;
		private IDesktop customDesktop;
		private IDesktop originalDesktop;

		public KioskManager(
			ILogger logger,
			IDesktopFactory desktopFactory,
			IDesktopMonitor desktopMonitor,
			IExplorerShell explorerShell,
			IProcessFactory processFactory)
		{
			this.logger = logger;
			this.desktopFactory = desktopFactory;
			this.desktopMonitor = desktopMonitor;
			this.explorerShell = explorerShell;
			this.processFactory = processFactory;
		}

		/// <summary>
		/// Currently active kiosk mode, or null when none is active.
		/// </summary>
		public KioskMode? ActiveMode => activeMode;

		/// <summary>
		/// Global instance (set by composition root) so UI components can call into the manager.
		/// </summary>
		public static KioskManager Instance { get; set; }

		/// <summary>
		/// Convenience wrapper to enable the default kiosk mode (CreateNewDesktop).
		/// </summary>
		public void EnableKiosk()
		{
			Enable(KioskMode.CreateNewDesktop);
		}

		/// <summary>
		/// Convenience wrapper to disable any active kiosk mode.
		/// </summary>
		public void DisableKiosk()
		{
			Disable();
		}

		/// <summary>
		/// Enable the requested kiosk mode. If another kiosk mode is active it will be reverted first.
		/// </summary>
		
        public void Enable(KioskMode mode)
		{
			if (mode == KioskMode.None)
			{
				logger.Info("Requested to enable kiosk mode 'None' - no action taken.");
				return;
			}

			if (activeMode == mode)
			{
				logger.Info($"Kiosk mode '{mode}' is already active.");
				return;
			}

			// If a different mode is active, revert it first
			if (activeMode != null)
			{
                logger.Info($"Reverting currently active kiosk mode '{activeMode}' before enabling '{mode}'...");
				Disable();
			}

			switch (mode)
			{
				case KioskMode.CreateNewDesktop:
					//CreateCustomDesktop();
					break;
				case KioskMode.DisableExplorerShell:
					//TerminateExplorerShell();
					break;
				default:
					logger.Warn($"Requested to enable unknown kiosk mode '{mode}'. No action taken.");
					return;
			}

			activeMode = mode;
			logger.Info($"Enabled kiosk mode '{mode}'.");
		}

		/// <summary>
		/// Disable any currently active kiosk mode and restore the system to its original state.
	/// </summary>
        public void Disable()
		{
			if (activeMode == null)
			{
				logger.Info("No kiosk mode active - nothing to disable.");
				return;
			}

			switch (activeMode)
			{
				case KioskMode.CreateNewDesktop:
					CloseCustomDesktop();
					break;
				case KioskMode.DisableExplorerShell:
					RestartExplorerShell();
					break;
			}

			logger.Info($"Disabled kiosk mode '{activeMode}'.");
			activeMode = null;
		}

		private void CreateCustomDesktop()
		{
			originalDesktop = desktopFactory.GetCurrent();
			logger.Info($"Current desktop is {originalDesktop}.");

			customDesktop = desktopFactory.CreateRandom();
			logger.Info($"Created custom desktop {customDesktop}.");

			customDesktop.Activate();
			processFactory.StartupDesktop = customDesktop;
			logger.Info("Successfully activated custom desktop.");

			desktopMonitor.Start(customDesktop);
		}

		private void CloseCustomDesktop()
		{
			desktopMonitor.Stop();

			if (originalDesktop != default)
			{
				originalDesktop.Activate();
				processFactory.StartupDesktop = originalDesktop;
				logger.Info($"Switched back to original desktop {originalDesktop}.");
			}
			else
			{
				logger.Warn($"No original desktop found to activate!");
			}

			if (customDesktop != default)
			{
				customDesktop.Close();
				logger.Info($"Closed custom desktop {customDesktop}.");
			}
			else
			{
				logger.Warn($"No custom desktop found to close!");
			}
		}

		private void TerminateExplorerShell()
		{
			explorerShell.HideAllWindows();
			explorerShell.Terminate();
		}

		private void RestartExplorerShell()
		{
			explorerShell.Start();
			explorerShell.RestoreAllWindows();
		}
	}
}
