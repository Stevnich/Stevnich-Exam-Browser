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
using SafeExamBrowser.WindowsApi.Contracts;

namespace SafeExamBrowser.WindowsApi.Desktops
{
	internal class Desktop : IDesktop
	{
		public IntPtr Handle { get; private set; }
		public string Name { get; private set; }

		public Desktop(IntPtr handle, string name)
		{
			Handle = handle;
			Name = name;
		}

		public void Activate()
		{
            // Activation of other desktops is disabled to prevent switching
			// away from the user's primary desktop.
			throw new NotSupportedException("Activating alternate desktops is disabled in this build.");
		}

		public void Close()
		{
			var success = User32.CloseDesktop(Handle);

			if (!success)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}

		public override string ToString()
		{
			return $"'{Name}' [{Handle}]";
		}
	}
}
