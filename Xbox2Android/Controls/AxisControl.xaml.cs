using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Xbox2Android.Controls
{
	public partial class AxisControl : UserControl, IDisplayControl
	{
		public static readonly SolidColorBrush UnselectedBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 255));
		public static readonly SolidColorBrush SelectedBrush = new SolidColorBrush(Color.FromArgb(192, 0, 192, 192));

		public AxisControl(TouchProfile profile)
		{
			InitializeComponent();
			shapeBackground.Fill = UnselectedBrush;
			textAxisRadius.Text = profile.AxisRadius.ToString();

			if (profile.ShadowAxisOffset.HasValue && profile.ShadowAxisOffset.Value != 0) {
				menuUseShadowAxis.IsChecked = true;
				menuShadowAxis.Visibility = Visibility.Visible;
				textShadowAxisOffset.Text = profile.ShadowAxisOffset.Value.ToString();
				shapeShadowAxisIn.Visibility = Visibility.Visible;
				shapeShadowAxisOut.Visibility = Visibility.Visible;
			}
			else {
				textShadowAxisOffset.Text = "-8";
			}
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

		public int AxisRadius
		{
			get { return -(int)matrixTranslate.X; }
			set
			{
				Width = value * 2;
				Height = value * 2;
				matrixTranslate.X = -value;
				matrixTranslate.Y = -value;
			}
		}

		public bool UseShadowAxis => menuUseShadowAxis.IsChecked;

		public int ShadowAxisOffset
		{
			get { return (int)shapeShadowAxisIn.Margin.Left; }
			set
			{
				shapeShadowAxisIn.Margin = new Thickness(value, 0, -value, 0);
				shapeShadowAxisOut.Margin = new Thickness(value, 0, -value, 0);
			}
		}

		public event EventHandler RemoveClicked;

		private void MenuRemove_Click(object sender, RoutedEventArgs e)
		{
			RemoveClicked?.Invoke(this, e);
		}

		private void TextAxisRadius_TextChanged(object sender, TextChangedEventArgs e)
		{
			ushort value;
			if (ushort.TryParse(textAxisRadius.Text, out value) && value >= 10) {
				AxisRadius = value;
			}
		}

		private void TextShadowAxisOffset_TextChanged(object sender, TextChangedEventArgs e)
		{
			short value;
			if (short.TryParse(textShadowAxisOffset.Text, out value)) {
				ShadowAxisOffset = value;
			}
		}

		private void MenuUseShadowAxis_Click(object sender, RoutedEventArgs e)
		{
			menuUseShadowAxis.IsChecked = !menuUseShadowAxis.IsChecked;
			menuShadowAxis.Visibility = menuUseShadowAxis.IsChecked ? Visibility.Visible : Visibility.Collapsed;
			shapeShadowAxisIn.Visibility = menuUseShadowAxis.IsChecked ? Visibility.Visible : Visibility.Collapsed;
			shapeShadowAxisOut.Visibility = menuUseShadowAxis.IsChecked ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
