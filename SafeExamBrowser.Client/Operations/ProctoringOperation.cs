/*
 * Copyright (c) 2026 ETH Zürich, IT Services
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using SafeExamBrowser.Core.Contracts.OperationModel;
using SafeExamBrowser.Core.Contracts.OperationModel.Events;
using SafeExamBrowser.I18n.Contracts;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.Proctoring.Contracts;
using SafeExamBrowser.UserInterface.Contracts;
using SafeExamBrowser.UserInterface.Contracts.Shell;
using System;
using System.IO;
using System.Windows;

namespace SafeExamBrowser.Client.Operations
{
	internal class ProctoringOperation : ClientOperation
	{
		private readonly IActionCenter actionCenter;
		private readonly IProctoringController controller;
		private readonly ILogger logger;
		private readonly ITaskbar taskbar;
		private readonly IUserInterfaceFactory uiFactory;

		public override event StatusChangedEventHandler StatusChanged;

		public ProctoringOperation(
			IActionCenter actionCenter,
			ClientContext context,
			IProctoringController controller,
			ILogger logger,
			ITaskbar taskbar,
			IUserInterfaceFactory uiFactory) : base(context)
		{
			this.actionCenter = actionCenter;
			this.controller = controller;
			this.logger = logger;
			this.taskbar = taskbar;
			this.uiFactory = uiFactory;
		}

		public override OperationResult Perform()
		{
			if (Context.Settings.Proctoring.Enabled)
			{
				logger.Info("Initializing proctoring...");
				StatusChanged?.Invoke(TextKey.OperationStatus_InitializeProctoring);

				controller.Initialize(Context.Settings.Proctoring);

				foreach (var notification in controller.Notifications)
				{
					initDummyProctor();
				}
			}

			return OperationResult.Success;
		}
		void initDummyProctor()
		{
			string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dir = Path.Combine(roaming, "SafeExamBrowser", "p_cfg");

			// Pastikan folder p_cfg sudah ada
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

			string filePath = Path.Combine(dir, "proctoring");

			if (!File.Exists(filePath))
			{
				// JALUR 1: File belum ada, buat baru berdasarkan setting awal
				string initialState = Context.Settings.Proctoring.Enabled ? "active" : "inactive";
				File.WriteAllText(filePath, initialState);
			}
			else
			{
				// JALUR 2: File sudah ada, baca status terakhir yang tersimpan
				string savedState = File.ReadAllText(filePath).Trim().ToLower();

				// Cek jika file kosong, isi ulang dengan default
				if (string.IsNullOrEmpty(savedState))
				{
					savedState = Context.Settings.Proctoring.Enabled ? "active" : "inactive";
					File.WriteAllText(filePath, savedState);
				}
			}
		}
		public override OperationResult Revert()
		{
			if (Context.Settings.Proctoring.Enabled)
			{
				logger.Info("Terminating proctoring...");
				StatusChanged?.Invoke(TextKey.OperationStatus_TerminateProctoring);

				controller.Terminate();

				foreach (var notification in controller.Notifications)
				{
					notification.Terminate();
				}
			}

			return OperationResult.Success;
		}
	}
}
