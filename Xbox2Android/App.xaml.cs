using System;
using System.Threading;
using System.Windows;

namespace Xbox2Android
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();
		}

		[STAThread]
		public static void Main()
		{
			bool isCreated;
			using (var mutex = new Mutex(true, "{11A6511F-E9BA-4D5B-85F4-0020BA3D7D92}", out isCreated)) {
				if (isCreated) {
					var app = new App();
					var mainWindow = new MainWindow();
					app.Run(mainWindow);
				}
			}
		}
	}
}
