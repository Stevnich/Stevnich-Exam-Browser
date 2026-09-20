using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace SafeExamBrowser.UserInterface.Desktop.Windows
{
	/// <summary>
	/// Interaction logic for BlackWindow.xaml
	/// </summary>
	public partial class BlackWindow : Window
	{
		public BlackWindow()
		{
			InitializeComponent();
			this.Width = SystemParameters.PrimaryScreenWidth;
			this.Height = SystemParameters.PrimaryScreenHeight;
			this.Left = 0;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			this.Close();
        }
    }
}
