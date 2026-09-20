using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SebPatcherTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		private void Application_Startup(object sender, StartupEventArgs e)
		{
          IThemeLoader.ApplyTheme();

			SystemEvents.UserPreferenceChanged += (s, ev) =>
			{
				if (ev.Category == UserPreferenceCategory.General)
				{
					IThemeLoader.ApplyTheme();
				}
			};

			// Create and show the main window (Configurator) so the UI appears.
			var mainWindow = new Configurator();
			this.MainWindow = mainWindow;
			mainWindow.Show();
		}
	}
}
