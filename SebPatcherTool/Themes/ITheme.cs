using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;

public static class IThemeLoader
{
	public static void ApplyTheme()
	{
		bool isLight = true;

		try
		{
			using (var key = Registry.CurrentUser.OpenSubKey(
				@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
			{
				if (key != null)
				{
					object value = key.GetValue("AppsUseLightTheme");
					if (value != null)
						isLight = (int) value > 0;
				}
			}
		}
		catch { }

		string theme = isLight
			? "Themes/Light.xaml"
			: "Themes/Dark.xaml";

		var newDict = new ResourceDictionary
		{
			Source = new Uri(theme, UriKind.Relative)
		};

		var dictionaries = Application.Current.Resources.MergedDictionaries;

		var oldTheme = dictionaries.FirstOrDefault(d =>
			d.Source != null &&
			d.Source.OriginalString.Contains("Themes/"));

		if (oldTheme != null)
			dictionaries.Remove(oldTheme);

		dictionaries.Add(newDict);
	}
}