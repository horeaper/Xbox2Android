using System.Windows;

namespace Xbox2Android
{
	public partial class TextInputWindow : Window
	{
		public string Result { get; private set; }

		public TextInputWindow(Window owner, string title, string content = "")
		{
			InitializeComponent();
			Owner = owner;
			Title = title;
			textContent.Text = content;
		}

		void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			textContent.SelectAll();
			textContent.Focus();
		}

		void ButtonOK_OnClick(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty(textContent.Text)) {
				Result = textContent.Text;
				DialogResult = true;
			}
		}
	}
}
