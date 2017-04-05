using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Xbox2Android.Controls
{
	public partial class DirectionControl : UserControl, IDisplayControl
	{
		public static readonly SolidColorBrush UnselectedBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 255));
		public static readonly SolidColorBrush SelectedBrush = new SolidColorBrush(Color.FromArgb(192, 0, 192, 192));

		public DirectionControl(TouchProfile profile)
		{
			InitializeComponent();
			shapeBackgroundH.Fill = UnselectedBrush;
			shapeBackgroundV.Fill = UnselectedBrush;
			textDirectionSpeed.Text = profile.DirectionSpeed.ToString();
		}

		public bool IsSelected
		{
			get { return ReferenceEquals(shapeBackgroundH.Fill, SelectedBrush); }
			set { shapeBackgroundH.Fill = shapeBackgroundV.Fill = value ? SelectedBrush : UnselectedBrush; }
		}

		public Point Location
		{
			get { return new Point(Canvas.GetLeft(this), Canvas.GetTop(this)); }
			set { Canvas.SetLeft(this, value.X); Canvas.SetTop(this, value.Y); }
		}

		public int DirectionSpeed { get; set; }

		public event EventHandler RemoveClicked;

		private void MenuRemove_Click(object sender, RoutedEventArgs e)
		{
			RemoveClicked?.Invoke(this, e);
		}

		void TextDirectionSpeed_TextChanged(object sender, TextChangedEventArgs e)
		{
			int value;
			if (int.TryParse(textDirectionSpeed.Text, out value) ) {
				DirectionSpeed = value;
			}
		}
	}
}
