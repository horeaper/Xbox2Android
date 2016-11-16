using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
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
		string m_inputEventPath = "";
		int m_deviceType;
		int m_width;
		int m_height;

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
			if (string.IsNullOrEmpty(m_inputEventPath)) {
				GetTouchInputEvent();
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
			if (string.IsNullOrEmpty(m_inputEventPath)) {
				var dialog = new AlertDialog.Builder(this);
				dialog.SetMessage("Unable to identify input event ╮(╯▽╰)╭\nPlease make sure the device is rooted.");
				dialog.SetNeutralButton("OK", (s, ev) => { });
				dialog.Show();
			}
			else if (textName.Text.Length > 0 && textIP.Text.Length > 0 && IPAddress.TryParse(textIP.Text, out address)) {
				Save();
				StartService(new Intent(this, typeof(NetworkService)));
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
		}

		void Load()
		{
			try {
				var settingPath = Path.Combine(GetExternalFilesDir(null).AbsolutePath, "settings.xml");
				var document = XDocument.Load(settingPath);
				var element = document.Root;
				textName.Text = element.Attribute("Name").Value;
				textIP.Text = element.Attribute("IP").Value;
				m_inputEventPath = element.Attribute("InputEventPath").Value;
				m_deviceType = int.Parse(element.Attribute("DeviceType").Value);
				m_width = int.Parse(element.Attribute("Width").Value);
				m_height = int.Parse(element.Attribute("Height").Value);
			}
			catch {
				// ignored
			}
		}

		void Save()
		{
			var element = new XElement("Data");
			element.SetAttributeValue("Name", textName.Text);
			element.SetAttributeValue("IP", textIP.Text);
			element.SetAttributeValue("InputEventPath", m_inputEventPath);
			element.SetAttributeValue("DeviceType", m_deviceType);
			element.SetAttributeValue("Width", m_width);
			element.SetAttributeValue("Height", m_height);
			var document = new XDocument(element);
			var settingPath = Path.Combine(GetExternalFilesDir(null).AbsolutePath, "settings.xml");
			document.Save(settingPath);
		}

		public void SetServiceRunning(bool isRunning)
		{
			textName.Enabled = !isRunning;
			textIP.Enabled = !isRunning;
			buttonConnect.Enabled = !isRunning;
			buttonStop.Enabled = isRunning;
		}

		void GetTouchInputEvent()
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
				return;
			}
			var lines = inputDeviceDescription.Split('\n');
			int colonIndex = lines[0].IndexOf(':');
			if (colonIndex == -1) {
				return;
			}

			m_inputEventPath = lines[0].Substring(colonIndex + 1).Trim();
			m_deviceType = inputDeviceDescription.Contains("ABS_MT_SLOT") ? 1 : 0;

			Func<string, int> fnGetMaxValue = identifier => {
				string desc = lines.First(text => text.Contains(identifier));
				int startIndex = desc.IndexOf("max") + 3;
				int endIndex = desc.IndexOf(',', startIndex);
				string maxContent = desc.Substring(startIndex, endIndex - startIndex);
				return int.Parse(maxContent);
			};
			m_width = fnGetMaxValue("ABS_MT_POSITION_X");
			m_height = fnGetMaxValue("ABS_MT_POSITION_Y");
		}
	}
}
