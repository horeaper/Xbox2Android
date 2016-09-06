using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Java.IO;

namespace Xbox2AndroidClient
{
	[Activity(Label = "Xbox2Android", MainLauncher = true, Icon = "@drawable/ic_launcher")]
	public class MainActivity : Activity
	{
		EditText textName;
		EditText textIP;
		Button buttonConnect;
		Button buttonStop;

		public static MainActivity Instance { get; private set; }
		public static string Name { get; private set; }
		public static IPAddress IP { get; private set; }
		public static string InputEvent { get; private set; }
		public const int ServerPort = 21499;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);
			Instance = this;

			textName = FindViewById<EditText>(Resource.Id.textName);
			textIP = FindViewById<EditText>(Resource.Id.textIP);
			buttonConnect = FindViewById<Button>(Resource.Id.buttonConnect);
			buttonStop = FindViewById<Button>(Resource.Id.buttonStop);
			buttonConnect.Click += ButtonConnect_Click;
			buttonStop.Click += ButtonStop_Click;

			Load();
			SetServiceRunning(NetworkService.IsRunning);
			if (string.IsNullOrEmpty(InputEvent)) {
				InputEvent = GetTouchInputEvent();
			}
		}

		protected override void OnDestroy()
		{
			Instance = null;
			base.OnDestroy();
		}

		public override void OnBackPressed()
		{
			Save();
			Finish();
		}

		private void ButtonConnect_Click(object sender, EventArgs e)
		{
			IPAddress address;
			if (string.IsNullOrEmpty(InputEvent)) {
				var dialog = new AlertDialog.Builder(this);
				dialog.SetMessage("Unable to identify input event ╮(╯▽╰)╭\nPlease make sure the device is rooted.");
				dialog.SetNeutralButton("OK", (s, ev) => { });
				dialog.Show();
			}
			else if (textName.Text.Length > 0 && textIP.Text.Length > 0 && IPAddress.TryParse(textIP.Text, out address)) {
				Name = textName.Text;
				IP = address;
				StartService(new Intent(this, typeof(NetworkService)));
				SetServiceRunning(true);
			}
			else {
				var dialog = new AlertDialog.Builder(this);
				dialog.SetMessage("Invalid name or IP specified.");
				dialog.SetNeutralButton("OK", (s, ev) => { });
				dialog.Show();
			}
		}

		private void ButtonStop_Click(object sender, EventArgs e)
		{
			StopService(new Intent(this, typeof(NetworkService)));
			SetServiceRunning(false);
		}

		void Load()
		{
			using (var pref = GetPreferences(FileCreationMode.Private)) {
				textName.Text = pref.GetString("Name", "");
				textIP.Text = pref.GetString("IP", "");
				InputEvent = pref.GetString("InputDevice", "");
			}
		}

		void Save()
		{
			using (var pref = GetPreferences(FileCreationMode.Private)) {
				var editor = pref.Edit();
				editor.PutString("Name", textName.Text);
				editor.PutString("IP", textIP.Text);
				editor.PutString("InputDevice", InputEvent);
				editor.Commit();
			}
		}

		public void SetServiceRunning(bool isRunning)
		{
			textName.Enabled = !isRunning;
			textIP.Enabled = !isRunning;
			buttonConnect.Enabled = !isRunning;
			buttonStop.Enabled = isRunning;
		}

		string GetTouchInputEvent()
		{
			var process = Java.Lang.Runtime.GetRuntime().Exec("su");
			var os = new DataOutputStream(process.OutputStream);
			os.WriteBytes("getevent -pl\n");
			os.Flush();
			os.WriteBytes("exit\n");
			os.Flush();
			process.WaitFor();

			var reader = new BufferedReader(new InputStreamReader(process.InputStream));
			var output = new System.Text.StringBuilder();
			while (true) {
				string read = reader.ReadLine();
				if (string.IsNullOrEmpty(read)) {
					break;
				}
				output.AppendLine(read);
			}
			process.Dispose();
			string content = output.ToString();

			var deviceCollection = new List<string>();
			var builder = new System.Text.StringBuilder();
			foreach (var line in content.Split('\n')) {
				if (line.Length > 0) {
					if (line[0] != ' ') {
						if (builder.Length > 0) {
							deviceCollection.Add(builder.ToString());
							builder.Clear();
						}
					}
					builder.Append(line);
					builder.Append('\n');
				}
			}

			string inputDeviceDescription = deviceCollection.FirstOrDefault(text => text.Contains("ABS_MT_POSITION_X") && text.Contains("ABS_MT_POSITION_Y"));
			if (inputDeviceDescription == null) {
				return "";
			}
			var lines = inputDeviceDescription.Split('\n');
			int colonIndex = lines[0].IndexOf(':');
			if (colonIndex == -1) {
				return "";
			}
			return lines[0].Substring(colonIndex + 1).Trim();
		}
	}
}
