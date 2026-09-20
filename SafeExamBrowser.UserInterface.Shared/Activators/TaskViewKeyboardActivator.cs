using System.Windows.Input;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.UserInterface.Contracts.Shell;
using SafeExamBrowser.UserInterface.Contracts.Shell.Events;
using SafeExamBrowser.WindowsApi.Contracts;
using SafeExamBrowser.WindowsApi.Contracts.Events;


namespace SafeExamBrowser.UserInterface.Shared.Activators
{
    public class TaskviewKeyboardActivator : KeyboardActivator, ITaskviewActivator
	{
		private bool Activated, LeftShift, Tab;
		// When true, allow the user to switch applications (do not block Alt+Tab / gestures)
		// Default to true so switching is allowed unless explicitly disabled.
		private bool allowAppSwitch = false;

		public bool AllowAppSwitch
		{
			get => allowAppSwitch;
			set
			{
				if (allowAppSwitch == value)
				{
					return;
				}

				allowAppSwitch = value;
				// Propagate to global flag so other activators won't register hooks.
				KeyboardActivator.GlobalAllowAppSwitch = allowAppSwitch;

				if (allowAppSwitch)
				{
					try
					{
						base.Stop();
					}
					catch
					{
					}
					logger?.Debug("AllowAppSwitch enabled; keyboard hook stopped.");
				}
				else
				{
					try
					{
						base.Start();
					}
					catch
					{
					}
					logger?.Debug("AllowAppSwitch disabled; keyboard hook started.");
				}
			}
		}

		private ILogger logger;

		public event ActivatorEventHandler Deactivated;
		public event ActivatorEventHandler NextActivated;
		public event ActivatorEventHandler PreviousActivated;

        public TaskviewKeyboardActivator(ILogger logger, INativeMethods nativeMethods) : base(nativeMethods)
		{
			this.logger = logger;

			// Ensure global flag reflects initial value so other activators won't
			// register hooks if app switching should be allowed by default.
			KeyboardActivator.GlobalAllowAppSwitch = allowAppSwitch;

			// Ensure the keyboard hook is not active when app switching is allowed by default.
			// The base Start() honors ShouldRegisterHook(), so no hook will be registered
			// if AllowAppSwitch is true. Explicitly call Stop() to be defensive in case
			// a hook was previously registered.
			if (allowAppSwitch)
			{
				try
				{
					base.Stop();
				}
				catch
				{
				}
			}
		}

		protected override void OnBeforePause()
		{
			if (Activated)
			{
				logger.Debug("Auto-deactivation.");
				Deactivated?.Invoke();
			}

			Activated = false;
		}

		protected override void OnBeforeResume()
		{
			Activated = false;
			LeftShift = false;
			Tab = false;
		}

		protected override bool Process(Key key, KeyModifier modifier, KeyState state)
		{
           // If application switching is allowed, do not block any key events.
			if (AllowAppSwitch)
			{
				return false;
			}
			if (IsDeactivation(modifier))
			{
				return false;
			}
			
			if (IsActivation(key, modifier, state))
			{
				return true;
			}

			return false;
		}
		
		private bool IsActivation(Key key, KeyModifier modifier, KeyState state)
		{
			var changed = false;
			var pressed = state == KeyState.Pressed && modifier.HasFlag(KeyModifier.Alt);

			switch (key)
			{
				case Key.Tab:
					changed = Tab != pressed;
					Tab = pressed;
					break;
				case Key.LeftShift:
					changed = LeftShift != pressed;
					LeftShift = pressed;
					break;
			}

			var isActivation = Tab && changed;

			if (isActivation)
			{
				Activated = true;

				if (LeftShift)
				{
					logger.Debug("Detected sequence for previous instance.");
					PreviousActivated?.Invoke();
				}
				else
				{
					logger.Debug("Detected sequence for next instance.");
					NextActivated?.Invoke();
				}
			}

			return isActivation;
		}
		
		private bool IsDeactivation(KeyModifier modifier)
		{
			var isDeactivation = Activated && !modifier.HasFlag(KeyModifier.Alt);

			if (isDeactivation)
			{
				Activated = false;
				LeftShift = false;
				Tab = false;

				logger.Debug("Detected deactivation sequence.");
				Deactivated?.Invoke();
			}

			return isDeactivation;
		}
	}
}
