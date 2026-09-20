using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SebPatcherTool
{
	public partial class numericUpDown : UserControl
	{
		public int MinValue { get; set; } = 0;
		public int MaxValue { get; set; } = 100;

		private DispatcherTimer _repeatTimer;
		private bool _isIncrementing;
		private Point _startPoint;
		private bool _isSliding;
		private int _valueAtStart;

		public int Value
		{
			get => int.TryParse(txtValue.Text, out int val) ? val : MinValue;
			set => txtValue.Text = Math.Max(MinValue, Math.Min(MaxValue, value)).ToString();
		}

		public numericUpDown()
		{
			InitializeComponent();
			InitializeTimer();

			// Event untuk Sliding pada TextBox
			txtValue.PreviewMouseDown += TxtValue_PreviewMouseDown;
			txtValue.PreviewMouseMove += TxtValue_PreviewMouseMove;
			txtValue.PreviewMouseUp += TxtValue_PreviewMouseUp;
		}

		#region Fitur Tekan Tahan (Button)
		private void InitializeTimer()
		{
			_repeatTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
			_repeatTimer.Tick += (s, e) => { if (_isIncrementing) Value++; else Value--; };
		}

		private void Up_MouseDown(object sender, MouseButtonEventArgs e) { _isIncrementing = true; Value++; _repeatTimer.Start(); }
		private void Down_MouseDown(object sender, MouseButtonEventArgs e) { _isIncrementing = false; Value--; _repeatTimer.Start(); }
		private void StopTimer(object sender, MouseEventArgs e) => _repeatTimer.Stop();
		#endregion

		#region Fitur Sliding (Horizontal Drag)
		private void TxtValue_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				_isSliding = true;
				_startPoint = e.GetPosition(this);
				_valueAtStart = Value;
				txtValue.Cursor = Cursors.SizeWE; // Ubah kursor ke panah kiri-kanan
				txtValue.CaptureMouse();
			}
		}

		private void TxtValue_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (_isSliding)
			{
				Point currentPoint = e.GetPosition(this);
				double delta = currentPoint.X - _startPoint.X;

				// Setiap 10 pixel geser = 1 poin perubahan
				int change = (int) (delta / 10);
				Value = _valueAtStart + change;
			}
		}

		private void TxtValue_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			_isSliding = false;
			txtValue.Cursor = Cursors.IBeam; // Kembalikan kursor ke mode teks
			txtValue.ReleaseMouseCapture();
		}
		#endregion

		// Event standar lainnya
		private void Up_Click(object sender, RoutedEventArgs e) => Value++;
		private void Down_Click(object sender, RoutedEventArgs e) => Value--;
		private void txtValue_MouseWheel(object sender, MouseWheelEventArgs e) { if (e.Delta > 0) Value++; else Value--; }
		private void txtValue_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !char.IsDigit(e.Text, 0);
		private void txtValue_TextChanged(object sender, TextChangedEventArgs e) { if (int.TryParse(txtValue.Text, out int val)) Value = val; }
	}
}