using Microsoft.Win32;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace SebPatcherTool
{
    /// <summary>
    /// Interaction logic for Configurator.xaml
    /// </summary>
	/// 
    public partial class Configurator : Window
    {
		public Configurator()
		{
			InitializeComponent();
			IThemeLoader.ApplyTheme();
			string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dir = Path.Combine(roaming, "SafeExamBrowser\\p_cfg");
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
		}

        void setMsgDispDelay()
		{
			string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dir = Path.Combine(roaming, "SafeExamBrowser");
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			string filePath = Path.Combine(dir, "p_cfg\\" +  "msgDelay");
			using (var writer = new StreamWriter(filePath, false))
			{
				writer.WriteLine(notificationPopupDelay.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}
		}
		void initShowDurationPref()
		{
			string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dir = Path.Combine(roaming, "SafeExamBrowser");
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			string filePath = Path.Combine(dir, "p_cfg\\" +  "actCenter");

			// Default values
			string[] defaultLines = { "2000", "600", "550" };

			// Jika file tidak ada, buat baru dengan nilai default
			if (!File.Exists(filePath))
			{
				File.WriteAllLines(filePath, defaultLines);
			}

			try
			{
				string[] lines = File.ReadAllLines(filePath);
				bool isValid = true;

				// Validasi: Harus ada minimal 3 baris
				if (lines.Length < 3)
				{
					isValid = false;
				}
				else
				{
					// Validasi: Cek apakah setiap baris benar-benar angka
					for (int i = 0; i < 3; i++)
					{
						if (!double.TryParse(lines[i], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
						{
							isValid = false;
							break;
						}
					}
				}

				if (!isValid)
				{
					// Jika isi file rusak/ngaco, timpa dengan default dan gunakan nilai default
					File.WriteAllLines(filePath, defaultLines);
					lines = defaultLines;
				}

				// Assign nilai ke variabel (aman karena sudah divalidasi di atas)
				actionCenterPopupDuration.Value = (int) double.Parse(lines[0], System.Globalization.CultureInfo.InvariantCulture);
				actionCenterAnimInDur.Value = (int) double.Parse(lines[1], System.Globalization.CultureInfo.InvariantCulture);
				actionCenterAnimOutDur.Value = (int) double.Parse(lines[2], System.Globalization.CultureInfo.InvariantCulture);
			}
			catch
			{
				// Jika terjadi error tak terduga (misal file dikunci sistem), biarkan nilai default di memory
			}
		}
			
		void initDummyProctor() {
			string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dir = Path.Combine(roaming, "SafeExamBrowser");
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			string filePath = Path.Combine(dir, "p_cfg\\" +  "proctoring");
			if (!File.Exists(filePath)) { File.Create(filePath).Close(); }

			try
			{
				string text;
				using (var reader = new StreamReader(filePath))
				{
					text = reader.ReadToEnd();
				}
            text = (text ?? string.Empty).Trim();
			string lower = text.ToLowerInvariant();

				// Check for "inactive" first because it contains "active" as a substring
				if (lower.Contains("inactive"))
			{
				proctorSetupActive.IsChecked = false;
				proctorSetupInactive.IsChecked = true;
				proctorSetupNone.IsChecked = false;
			}
			else if (lower.Contains("active"))
			{
				proctorSetupActive.IsChecked = true;
				proctorSetupInactive.IsChecked = false;
				proctorSetupNone.IsChecked = false;
			}
			else
			{
				proctorSetupActive.IsChecked = false;
				proctorSetupInactive.IsChecked = false;
				proctorSetupNone.IsChecked = true;
			}
			}
			catch (IOException e)
			{
				MessageBox.Show("The file could not be read:");
				Console.WriteLine(e.Message);
			}
		}
		void initWebDesignMode()
		{
			try
			{
				string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string dir = Path.Combine(roaming, "SafeExamBrowser", "p_cfg");
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

				string filePath = Path.Combine(dir, "edpg");
				if (!File.Exists(filePath))
				{
					chkMakeWebpagesEditable.IsChecked = false;
					chkOnlyAllowDesignModeDebug.IsChecked = false;
					return;
				}

				string text;
				using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (var reader = new StreamReader(stream))
				{
					text = reader.ReadToEnd().Trim().ToLowerInvariant();
				}

				// Perbaikan: Bandingkan dengan huruf kecil semua
				if (text == "onallstate")
				{
					chkMakeWebpagesEditable.IsChecked = true;
					chkOnlyAllowDesignModeDebug.IsChecked = false;
				}
				else if (text == "onlyindebug")
				{
					chkMakeWebpagesEditable.IsChecked = true;
					chkOnlyAllowDesignModeDebug.IsChecked = true;
				}
				else
				{
					// Jika file kosong atau berisi teks lain, 
					// set default (keduanya false atau sesuai keinginan)
					chkMakeWebpagesEditable.IsChecked = false;
					chkOnlyAllowDesignModeDebug.IsChecked = false;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error init: " + ex.Message);
			}
		}
		void saveDummyProctorState()
		{
			string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			if (proctorSetupActive.IsChecked == true)
			{
				string proctorFilePath = Path.Combine(roaming, "SafeExamBrowser\\p_cfg", "proctoring");
				File.WriteAllText(proctorFilePath, "active");
			}
			if (proctorSetupInactive.IsChecked == true)
			{
				string proctorFilePath = Path.Combine(roaming, "SafeExamBrowser\\p_cfg", "proctoring");
				File.WriteAllText(proctorFilePath, "inactive");
			}
			if (proctorSetupNone.IsChecked == true)
			{
				string proctorFilePath = Path.Combine(roaming, "SafeExamBrowser\\p_cfg", "proctoring");
				File.Delete(proctorFilePath);
			}
		}
		void saveWebDesignMode()
		{
			try
			{
				string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string configDir = Path.Combine(roaming, "SafeExamBrowser", "p_cfg");
				if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
				string filePath = Path.Combine(configDir, "edpg");

				if (chkMakeWebpagesEditable.IsChecked == true)
				{
					if (chkOnlyAllowDesignModeDebug.IsChecked == true)
					{
						File.WriteAllText(filePath, "onlyInDebug");
					}
					else
					{
						File.WriteAllText(filePath, "onAllState");
					}
				}
				else
				{
					File.Delete(filePath);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Gagal menyimpan konfigurasi: " + ex.Message);
			}
		}
		void saveShowDurationPref()
		{
			string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dir = Path.Combine(roaming, "SafeExamBrowser");
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			string filePath = Path.Combine(dir, "p_cfg\\" +  "actCenter");

			// Default values
			string[] defaultLines = { actionCenterPopupDuration.Value.ToString(), actionCenterAnimInDur.Value.ToString(), actionCenterAnimOutDur.Value.ToString() };

			File.WriteAllLines(filePath, defaultLines);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			windowTitle.Text = this.Title;
			initDummyProctor();
			initShowDurationPref();
			initWebDesignMode();
			string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string dir = Path.Combine(roaming, "SafeExamBrowser");
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			string filePath = Path.Combine(dir, "p_cfg\\" +  "msgDelay");
			if (!File.Exists(filePath)) { File.Create(filePath).Close(); }

			try
			{
				var content = File.ReadAllText(filePath).Trim();
				if (!string.IsNullOrEmpty(content))
				{
					if (double.TryParse(content, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
					{
						notificationPopupDelay.Value = (int) val;
					}
				}
			}
			catch
			{
				// ignore errors and keep default value
			}
		}
		private void Button_Click(object sender, RoutedEventArgs e)
		{
			saveDummyProctorState();
			setMsgDispDelay();
			saveShowDurationPref();
			saveWebDesignMode();
		}
		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			saveDummyProctorState();
			setMsgDispDelay();
			saveShowDurationPref();
			saveWebDesignMode();
			this.Close();
		}

		private void cancelBtn_Click(object sender, RoutedEventArgs e)
		{
			this.Close();

        }
    }
}
