using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace SafeExamBrowser.UserInterface.Desktop.Windows
{
	/// <summary>
	/// Interaction logic for Popup.xaml
	/// </summary>
	public partial class Popup : Window
	{
		public Popup()
		{
			InitializeComponent();
			WindowStyle = WindowStyle.None;
			ResizeMode = ResizeMode.NoResize;
			ShowInTaskbar = false;
			Topmost = true;
			AllowsTransparency = true;
			Background = Brushes.Transparent;

			Loaded += Popup_Loaded;
			Closing += Popup_Closing;
		}
		private void Popup_Loaded(object sender, RoutedEventArgs e)
		{
			var screen = SystemParameters.WorkArea;

			Width = screen.Width;
			Left = screen.Left;

			//posisi di bawah layar
			Top = screen.Bottom - Height;
			this.Show();
		}
		private void Popup_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;   // batalkan close
			this.Hide();       // hanya sembunyikan
		}
		public void ShowNotification(string pesan, NotificationType tipe, bool isTransparent)
		{
			notificationContent.Text = pesan;

			switch (tipe)
			{
				case NotificationType.Info:
					IconText.Text = "i";
					IconEllipse.Stroke = new SolidColorBrush(Color.FromRgb(34, 34, 34));
					break;

				case NotificationType.Question:
					IconText.Text = "?";
					IconEllipse.Stroke = Brushes.DeepSkyBlue;
					break;

				case NotificationType.Error:
					IconText.Text = "❌";
					IconEllipse.Stroke = Brushes.Crimson;
					break;
			}

			int showDurationMs = 2000;
			int animUpMs = 200;
			int animDownMs = 200;

			string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dir = Path.Combine(roaming, "SafeExamBrowser", "p_cfg");

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			string filePath = Path.Combine(dir, "msgDelay");

			if (!File.Exists(filePath))
			{
				File.WriteAllText(filePath,
					"2000\n300\n310"
				);
			}

			try
			{
				var lines = File.ReadAllLines(filePath);

				if (lines.Length > 0)
					int.TryParse(lines[0], out showDurationMs);

				if (lines.Length > 1)
					int.TryParse(lines[1], out animUpMs);

				if (lines.Length > 2)
					int.TryParse(lines[2], out animDownMs);
			}
			catch
			{
				// jika gagal baca, pakai default
			}

			NotificationPopup.Visibility = Visibility.Visible;
			NotificationPopup.Opacity = isTransparent ? 0.0 : 1.0;

			double animUpSec = animUpMs / 1000.0;
			double animDownSec = animDownMs / 1000.0;
			double stayVisibleSec = showDurationMs / 1000.0;

			Storyboard sb = new Storyboard();

			var easeOut = new QuadraticEase { EasingMode = EasingMode.EaseOut };
			var easeIn = new QuadraticEase { EasingMode = EasingMode.EaseIn };

			DoubleAnimation slideUp = new DoubleAnimation
			{
				From = 5,
				To = -160,
				Duration = TimeSpan.FromSeconds(animUpSec),
				EasingFunction = easeOut
			};

			Storyboard.SetTarget(slideUp, NotificationPopup);
			Storyboard.SetTargetProperty(slideUp,
				new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

			DoubleAnimation slideDown = new DoubleAnimation
			{
				To = 5,
				Duration = TimeSpan.FromSeconds(animDownSec),
				BeginTime = TimeSpan.FromSeconds(animUpSec + stayVisibleSec),
				EasingFunction = easeIn
			};

			Storyboard.SetTarget(slideDown, NotificationPopup);
			Storyboard.SetTargetProperty(slideDown,
				new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

			sb.Completed += (s, e) =>
			{
				NotificationPopup.Visibility = Visibility.Hidden;
			};

			sb.Children.Add(slideUp);
			sb.Children.Add(slideDown);
			sb.Begin();
			this.Show();
		}
		public enum NotificationType { Info, Question, Error }
	}
}
