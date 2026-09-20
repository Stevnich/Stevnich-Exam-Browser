using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SafeExamBrowser.Core.Contracts.Resources.Icons;
using SafeExamBrowser.I18n.Contracts;
using SafeExamBrowser.SystemComponents.Contracts.Audio;
using SafeExamBrowser.UserInterface.Contracts.Shell;
using SafeExamBrowser.UserInterface.Shared.Utilities;

namespace SafeExamBrowser.UserInterface.Desktop.Controls.Taskbar
{
	internal partial class AudioControl : UserControl, ISystemControl
	{
		private readonly IAudio audio;
		private readonly IText text;
		private bool muted;
		private IconResource MutedIcon;
		private IconResource NoDeviceIcon;
		private bool _isInternalUpdate = false;

		internal AudioControl(IAudio audio, IText text)
		{
			this.audio = audio;
			this.text = text;

			InitializeComponent();
			InitializeAudioControl();
		}

		public void Close()
		{
			Popup.IsOpen = false;
		}

		private void InitializeAudioControl()
		{
			var originalBrush = Button.Background;

			audio.VolumeChanged += Audio_VolumeChanged;
			Button.Click += (o, args) => Popup.IsOpen = !Popup.IsOpen;
			var lastOpenedBySpacePress = false;
			Button.PreviewKeyDown += (o, args) =>
			{
				// For some reason, the popup immediately closes again if opened by a Space Bar key event - as a mitigation,
				// we record the space bar event and leave the popup open for at least 3 seconds.
				if (args.Key == System.Windows.Input.Key.Space)
				{
					lastOpenedBySpacePress = true;
				}
			};
			Button.MouseLeave += (o, args) => Task.Delay(250).ContinueWith(_ => Dispatcher.Invoke(() =>
			{
				if (Popup.IsOpen && lastOpenedBySpacePress)
				{
					return;
				}
				Popup.IsOpen = Popup.IsMouseOver;
			}));
			MuteButton.Click += MuteButton_Click;
			MutedIcon = new XamlIconResource { Uri = new Uri("pack://application:,,,/SafeExamBrowser.UserInterface.Desktop;component/Images/Audio_Muted.xaml") };
			NoDeviceIcon = new XamlIconResource { Uri = new Uri("pack://application:,,,/SafeExamBrowser.UserInterface.Desktop;component/Images/Audio_NoDevice.xaml") };
			Popup.CustomPopupPlacementCallback = new CustomPopupPlacementCallback(Popup_PlacementCallback);
			Popup.MouseLeave += (o, args) => Task.Delay(250).ContinueWith(_ => Dispatcher.Invoke(() =>
			{
				if (Popup.IsOpen && lastOpenedBySpacePress)
				{
					return;
				}
				Popup.IsOpen = IsMouseOver;
			}));
			Volume.ValueChanged += Volume_ValueChanged;

			Popup.Opened += (o, args) =>
			{
				Background = Brushes.LightGray;
				Button.Background = Brushes.LightGray;
				Volume.Focus();
			};

			Popup.Closed += (o, args) =>
			{
				Background = originalBrush;
				Button.Background = originalBrush;
				lastOpenedBySpacePress = false;
			};

			if (audio.HasOutputDevice)
			{
				AudioDeviceName.Text = audio.DeviceFullName;
				Button.IsEnabled = true;
				UpdateVolume(audio.OutputVolume, audio.OutputMuted);
			}
			else
			{
				AudioDeviceName.Text = text.Get(TextKey.SystemControl_AudioDeviceNotFound);
				Button.IsEnabled = false;
				Button.ToolTip = text.Get(TextKey.SystemControl_AudioDeviceNotFound);
				ButtonIcon.Content = IconResourceLoader.Load(NoDeviceIcon);
			}
		}

		private void Audio_VolumeChanged(double volume, bool muted)
		{
			Dispatcher.InvokeAsync(() => UpdateVolume(volume, muted));
		}

		private void MuteButton_Click(object sender, RoutedEventArgs e)
		{
			if (muted)
			{
				audio.Unmute();
			}
			else
			{
				audio.Mute();
			}
		}

		private CustomPopupPlacement[] Popup_PlacementCallback(Size popupSize, Size targetSize, Point offset)
		{
			return new[]
			{
				new CustomPopupPlacement(new Point(targetSize.Width / 2 - popupSize.Width / 2, -popupSize.Height), PopupPrimaryAxis.None)
			};
		}

		private void Volume_DragStarted(object sender, DragStartedEventArgs e)
		{
			Volume.ValueChanged -= Volume_ValueChanged;
		}

		private void Volume_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			audio.SetVolume(Volume.Value / 100);
			Volume.ValueChanged += Volume_ValueChanged;
		}
		private void Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (_isInternalUpdate) return;

			// Update volume secara langsung ke backend
			audio.SetVolume(Volume.Value / 100);

			// Update teks tooltip/label tanpa memicu loop slider
			UpdateUIElementsOnly(Volume.Value / 100, this.muted);
		}

		private void UpdateVolume(double volume, bool muted)
		{
			this.muted = muted;
			var info = BuildInfoText(volume, muted);

			_isInternalUpdate = true; // Kunci event agar tidak loop
			try
			{
				Volume.Value = Math.Round(volume * 100);
				UpdateUIElementsOnly(volume, muted);
			}
			finally
			{
				_isInternalUpdate = false; // Buka kembali
			}
		}

		// Fungsi pembantu agar UI tetap update saat digeser/scroll tanpa loop
		private void UpdateUIElementsOnly(double volume, bool muted)
		{
			var info = BuildInfoText(volume, muted);
			Button.ToolTip = info;
			AutomationProperties.SetName(Button, info);

			if (muted)
			{
				PopupIcon.Content = IconResourceLoader.Load(MutedIcon);
				ButtonIcon.Content = IconResourceLoader.Load(MutedIcon);
			}
			else
			{
				PopupIcon.Content = LoadIcon(volume);
				ButtonIcon.Content = LoadIcon(volume);
			}
		}

		private string BuildInfoText(double volume, bool muted)
		{
			var info = text.Get(muted ? TextKey.SystemControl_AudioDeviceInfoMuted : TextKey.SystemControl_AudioDeviceInfo);

			info = info.Replace("%%NAME%%", audio.DeviceShortName);
			info = info.Replace("%%VOLUME%%", Convert.ToString(Math.Round(volume * 100)));

			return info;
		}

		private UIElement LoadIcon(double volume)
		{
			var icon = volume > 0.66 ? "100" : (volume > 0.33 ? "66" : "33");
			var uri = new Uri($"pack://application:,,,/SafeExamBrowser.UserInterface.Desktop;component/Images/Audio_{icon}.xaml");
			var resource = new XamlIconResource { Uri = uri };

			return IconResourceLoader.Load(resource);
		}

		private void Popup_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Escape)
			{
				Popup.IsOpen = false;
				Button.Focus();
			}
		}

		private void Volume_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				Popup.IsOpen = false;
				Button.Focus();
			}
		}

		private void Volume_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			audio.SetVolume(Volume.Value / 100);
			Volume.ValueChanged += Volume_ValueChanged;
		}

		private void Volume_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
		{
			ApplyMouseWheelVolume(e.Delta);
		}
		private void ApplyMouseWheelVolume(int delta)
		{
			const double step = 2.0;
			double currentVolume = Volume.Value;
			double newVolume = delta > 0 ? currentVolume + step : currentVolume - step;

			newVolume = Math.Max(0, Math.Min(100, newVolume));

			// Gunakan Async agar scroll terasa enteng
			Dispatcher.InvokeAsync(() => {
				Volume.Value = newVolume;

				if (delta > 0 && muted)
				{
					audio.Unmute();
				}
			}, System.Windows.Threading.DispatcherPriority.Input);
		}
	}
}
