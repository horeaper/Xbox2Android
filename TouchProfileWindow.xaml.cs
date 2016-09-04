using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Xbox2Android.Controls;
using Xbox2Android.Native;

namespace Xbox2Android
{
	/// <summary>
	/// Interaction logic for TouchProfileWindow.xaml
	/// </summary>
	public partial class TouchProfileWindow : Window
	{
		AxisControl m_axisControl;
		List<ButtonControl>[] m_buttonControls = new List<ButtonControl>[Constants.ButtonCount];
		List<ButtonControl> m_leftTriggerControls = new List<ButtonControl>();
		List<ButtonControl> m_rightTriggerControls = new List<ButtonControl>();

		string m_backgroundImage;
		Point m_mouseRightClickPoint;
		Vector m_mouseDragOffset;

		public TouchProfileWindow()
		{
			InitializeComponent();
			for (int index = 0; index < Constants.ButtonCount; ++index) {
				m_buttonControls[index] = new List<ButtonControl>();
			}

			if (!string.IsNullOrEmpty(MainWindow.Settings.BackgroundImage)) {
				m_backgroundImage = MainWindow.Settings.BackgroundImage;
				LoadBackgroundImage();
			}
			if (MainWindow.Settings.AxisCenter.HasValue) {
				AddAxisControlToCanvas();
				m_axisControl.Location = MainWindow.Settings.AxisCenter.Value;
			}
			for (int index = 0; index < Constants.ButtonCount; ++index) {
				foreach (var point in MainWindow.Settings.ButtonPositions[index]) {
					AddButtonControlToCanvas(new ButtonControl(Constants.ButtonDisplayName[index], point), m_buttonControls[index]);
				}
			}
		}

#region Functions

		void LoadBackgroundImage()
		{
			try {
				var fileStream = new MemoryStream(File.ReadAllBytes(m_backgroundImage));
				var bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.StreamSource = fileStream;
				bitmap.EndInit();

				imageBackground.Source = bitmap;
				imageBackground.Width = bitmap.PixelWidth;
				imageBackground.Height = bitmap.PixelHeight;
			}
			catch {
				m_backgroundImage = null;
				imageBackground.Source = null;
			}
		}

		void AddAxisControlToCanvas()
		{
			m_axisControl = new AxisControl();
			m_axisControl.MouseDown += IDisplayControl_MouseDown;
			m_axisControl.MouseUp += IDisplayControl_MouseUp;
			m_axisControl.RemoveClicked += AxisControl_RemoveClicked;
			canvasMain.Children.Add(m_axisControl);
		}

		void AddButtonControlToCanvas(ButtonControl control, List<ButtonControl> collection)
		{
			control.MouseDown += IDisplayControl_MouseDown;
			control.MouseUp += IDisplayControl_MouseUp;
			control.RemoveClicked += ButtonControl_RemoveClicked;
			collection.Add(control);
			canvasMain.Children.Add(control);
		}

#endregion

#region Window Events

		void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (m_axisControl != null) {
				m_axisControl.IsSelected = false;
			}
			foreach (var control in m_buttonControls.SelectMany(items => items).Concat(m_leftTriggerControls).Concat(m_rightTriggerControls)) {
				control.IsSelected = false;
			}
		}

		void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			m_mouseRightClickPoint = e.GetPosition(canvasMain);
		}

#endregion

#region Menu Events

		private void SetBackgroundImage_Click(object sender, RoutedEventArgs e)
		{
			var openFile = new OpenFileDialog();
			openFile.Filter = "Supported Image File (*.png, *.jpg, *.bmp)|*.png;*.jpg;*.bmp";
			openFile.RestoreDirectory = true;
			if (openFile.ShowDialog(this) == true) {
				m_backgroundImage = openFile.FileName;
				LoadBackgroundImage();
			}
		}

		private void SetAxis_Click(object sender, RoutedEventArgs e)
		{
			if (m_axisControl == null) {
				AddAxisControlToCanvas();
			}

			m_axisControl.Location = m_mouseRightClickPoint;
		}

		private void AxisControl_RemoveClicked(object sender, EventArgs e)
		{
			canvasMain.Children.Remove(m_axisControl);
			m_axisControl = null;
		}

		private void ClearAll_Click(object sender, RoutedEventArgs e)
		{
			foreach (var items in m_buttonControls) {
				items.Clear();
			}
			m_leftTriggerControls.Clear();
			m_rightTriggerControls.Clear();
			canvasMain.Children.Clear();
		}

		private void SaveAndExit_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.Settings.BackgroundImage = m_backgroundImage;
			if (m_axisControl != null) {
				MainWindow.Settings.AxisCenter = m_axisControl.Location;
				MainWindow.Settings.AxisRadius = m_axisControl.AxisRadius;
				MainWindow.Settings.ShadowAxisOffset = m_axisControl.ShadowAxisOffset;
			}
			for (int index = 0; index < Constants.ButtonCount; ++index) {
				MainWindow.Settings.ButtonPositions[index].Clear();
				MainWindow.Settings.ButtonPositions[index].AddRange(m_buttonControls[index].Select(control => control.Location));
			}

			Close();
		}

		private void ExitWithoutSaving_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

#endregion

#region Add Button Events

		private void SetControllerButton_Click(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var button = (XInput.GamePadButton)Enum.Parse(typeof(XInput.GamePadButton), menuItem.Header.ToString());
			int buttonId = Array.IndexOf(Constants.ButtonValue, button);

			var control = new ButtonControl(Constants.ButtonDisplayName[buttonId], m_mouseRightClickPoint);
			AddButtonControlToCanvas(control, m_buttonControls[buttonId]);
		}

		private void SetControllerLeftTrigger_Click(object sender, RoutedEventArgs e)
		{
			var control = new ButtonControl("LT", m_mouseRightClickPoint);
			AddButtonControlToCanvas(control, m_leftTriggerControls);
		}

		private void SetControllerRightTrigger_Click(object sender, RoutedEventArgs e)
		{
			var control = new ButtonControl("RT", m_mouseRightClickPoint);
			AddButtonControlToCanvas(control, m_rightTriggerControls);
		}

		private void ButtonControl_RemoveClicked(object sender, EventArgs e)
		{
			var selectedControl = (ButtonControl)sender;
			foreach (var items in m_buttonControls) {
				items.Remove(selectedControl);
			}
			m_leftTriggerControls.Remove(selectedControl);
			m_rightTriggerControls.Remove(selectedControl);
			canvasMain.Children.Remove(selectedControl);
		}

#endregion

#region Drag & Drop Events

		private void IDisplayControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			var selectedControl = (IDisplayControl)sender;

			if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right) {
				if (!selectedControl.IsSelected) {
					if (m_axisControl != null) {
						m_axisControl.IsSelected = false;
					}
					foreach (var control in m_buttonControls.SelectMany(items => items).Concat(m_leftTriggerControls).Concat(m_rightTriggerControls)) {
						control.IsSelected = false;
					}
					selectedControl.IsSelected = true;
					canvasMain.Children.Remove((UIElement)selectedControl);
					canvasMain.Children.Add((UIElement)selectedControl);
				}
			}

			if (e.ChangedButton == MouseButton.Left) {
				var mousePos = e.GetPosition(canvasMain);
				m_mouseDragOffset = mousePos - selectedControl.Location;
				Mouse.Capture(selectedControl);
				e.Handled = true;
			}
		}

		private void Canvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (Mouse.Captured != null) {
				var selectedControl = (IDisplayControl)Mouse.Captured;
				var mousePos = e.GetPosition(canvasMain);
				selectedControl.Location = mousePos - m_mouseDragOffset;
				e.Handled = true;
			}
		}

		private void IDisplayControl_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left) {
				Mouse.Capture(null);
				e.Handled = true;
			}
		}

#endregion
	}
}
