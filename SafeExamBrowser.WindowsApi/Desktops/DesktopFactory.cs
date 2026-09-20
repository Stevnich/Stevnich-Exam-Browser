/*
 * Copyright (c) 2026 ETH Zürich, IT Services
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.WindowsApi.Constants;
using SafeExamBrowser.WindowsApi.Contracts;

namespace SafeExamBrowser.WindowsApi.Desktops
{
	public class DesktopFactory : IDesktopFactory
	{
		private readonly ILogger logger;
		private readonly Random random;

		public DesktopFactory(ILogger logger)
		{
			this.logger = logger;
			this.random = new Random();
		}

		public IDesktop CreateNew(string name)
		{
            // Creation of alternate desktops is disabled to prevent the
			// application from creating isolated desktop environments which
			// could interfere with the user's shell.
			logger.Warn($"CreateNew('{name}') was called but desktop creation is disabled.");
			throw new NotSupportedException("Creating new desktops is disabled in this build.");
		}

		public IDesktop CreateRandom()
		{
            // Desktop creation is disabled; do not return an alternate desktop.
			logger.Warn("CreateRandom() was called but desktop creation is disabled.");
			throw new NotSupportedException("Creating random desktops is disabled in this build.");
		}

		public IDesktop GetCurrent()
		{
			var threadId = Kernel32.GetCurrentThreadId();
			var handle = User32.GetThreadDesktop(threadId);
			var name = string.Empty;
			var nameLength = 0;

			if (handle == IntPtr.Zero)
			{
				logger.Error($"Failed to get desktop handle for thread with ID = {threadId}!");

				throw new Win32Exception(Marshal.GetLastWin32Error());
			}

			logger.Debug($"Found desktop handle for thread with ID = {threadId}. Attempting to get desktop name...");

			User32.GetUserObjectInformation(handle, Constant.UOI_NAME, IntPtr.Zero, 0, ref nameLength);

			var namePointer = Marshal.AllocHGlobal(nameLength);
			var success = User32.GetUserObjectInformation(handle, Constant.UOI_NAME, namePointer, nameLength, ref nameLength);

			if (!success)
			{
				logger.Error($"Failed to retrieve name for desktop with handle = {handle}!");

				throw new Win32Exception(Marshal.GetLastWin32Error());
			}

			name = Marshal.PtrToStringAnsi(namePointer);
			Marshal.FreeHGlobal(namePointer);

			var desktop = new Desktop(handle, name);

			logger.Debug($"Successfully determined current desktop {desktop}.");

			return desktop;
		}

		private string GenerateRandomDesktopName()
		{
			var length = random.Next(5, 20);
			var name = new char[length];

			for (var letter = 0; letter < length; letter++)
			{
				name[letter] = (char) (random.Next(2) == 0 && letter != 0 ? random.Next('a', 'z' + 1) : random.Next('A', 'Z' + 1));
			}

			return new string(name);
		}
	}
}
