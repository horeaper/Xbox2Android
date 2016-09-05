using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Xbox2Android.Controls
{
	public partial class ButtonControl : UserControl, IDisplayControl
	{
		public static readonly SolidColorBrush UnselectedBrush = new SolidColorBrush(Color.FromArgb(192, 0, 255, 255));
		public static readonly SolidColorBrush SelectedBrush = new SolidColorBrush(Color.FromArgb(255, 0, 192, 192));

		public ButtonControl(string name, Point position)
		{
			InitializeComponent();
			Location = position;
			shapeBackground.Fill = UnselectedBrush;
			textTitle.Text = name;
		}

		public bool IsSelected
		{
			get { return ReferenceEquals(shapeBackground.Fill, SelectedBrush); }
			set { shapeBackground.Fill = value ? SelectedBrush : UnselectedBrush; }
		}

		public Point Location
		{
			get { return new Point(Canvas.GetLeft(this), Canvas.GetTop(this)); }
			set { Canvas.SetLeft(this, value.X); Canvas.SetTop(this, value.Y); }
		}

		public event EventHandler RemoveClicked;

		private void MenuRemove_Click(object sender, RoutedEventArgs e)
		{
			RemoveClicked?.Invoke(this, e);
		}
	}
}
