/*
 * Copyright (c) 2026 ETH Zürich, IT Services
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Windows.Input;
using SafeExamBrowser.WindowsApi.Contracts;
using SafeExamBrowser.WindowsApi.Contracts.Events;

namespace SafeExamBrowser.UserInterface.Shared.Activators
{
	public abstract class KeyboardActivator
	{
		private Guid? hookId;
		private INativeMethods nativeMethods;
		private bool paused;

		protected KeyboardActivator(INativeMethods nativeMethods)
		{
			this.nativeMethods = nativeMethods;
		}

		protected abstract bool Process(Key key, KeyModifier modifier, KeyState state);

		public void Pause()
		{
			OnBeforePause();
			paused = true;
		}

		public void Resume()
		{
			OnBeforeResume();
			paused = false;
		}

        public virtual void Start()
		{
          // Allow derived classes to prevent the hook from being registered
			// (for example when application switching should be allowed).
			
			if (!ShouldRegisterHook())
			{
				return;
			}

			hookId = nativeMethods.RegisterKeyboardHook(KeyboardHookCallback);
		}

        public virtual void Stop()
		{
			if (hookId.HasValue)
			{
				nativeMethods.DeregisterKeyboardHook(hookId.Value);
			}
		}

		protected virtual void OnBeforePause()
		{
		}

		protected virtual void OnBeforeResume()
		{
		}

		// Derived classes can override this to prevent the keyboard hook from
		// being registered by Start(). By default the hook is registered.
		protected virtual bool ShouldRegisterHook()
		{
            // Respect the global flag which can be used to allow app switching
			// across the entire process (prevents any keyboard hooks from being registered).
			return !GlobalAllowAppSwitch;
		}

		/// <summary>
		/// When set to true, all keyboard activators should refrain from registering
		/// a keyboard hook, effectively allowing application switching (Alt+Tab,
		/// gestures that are translated into keyboard events) globally.
		/// </summary>
       public static bool GlobalAllowAppSwitch { get; set; } = true;

		private bool KeyboardHookCallback(int keyCode, KeyModifier modifier, KeyState state)
		{
			if (!paused)
			{
				return Process(KeyInterop.KeyFromVirtualKey(keyCode), modifier, state);
			}

			return false;
		}
	}
}
