using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Xbox2Android.Controls
{
	public partial class TriggerModePanel : UserControl
	{
		public TriggerModePanel()
		{
			InitializeComponent();
		}

		public bool IsChecked
		{
			get { return radioName.IsChecked == true; }
			set { radioName.IsChecked = value; }
		}

		public string Text
		{
			get { return radioName.Content.ToString(); }
			set { radioName.Content = value; }
		}

		public int Value
		{
			get { return (int)sliderValue.Value; }
			set { sliderValue.Value = value; }
		}

		public int MaximumValue
		{
			get { return (int)sliderValue.Maximum; }
			set { sliderValue.Maximum = value; }
		}

		public event EventHandler Selected;

		void RadioName_OnChecked(object sender, RoutedEventArgs e)
		{
			Opacity = 1.0;
			Selected?.Invoke(this, EventArgs.Empty);
		}

		void RadioName_OnUnchecked(object sender, RoutedEventArgs e)
		{
			Opacity = 0.5;
		}

		void LayoutRoot_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (!MainWindow.IsLoading) {
				radioName.IsChecked = true;
			}
		}

		void SliderValue_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (!MainWindow.IsLoading) {
				radioName.IsChecked = true;
				Selected?.Invoke(this, EventArgs.Empty);
			}
			e.Handled = true;
		}
	}
}
