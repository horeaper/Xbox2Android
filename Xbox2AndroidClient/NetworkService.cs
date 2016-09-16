using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Java.IO;

namespace Xbox2AndroidClient
{
	[Service]
	public class NetworkService : Service
	{
		public const int ServerPort = 21499;
		public static bool IsRunning { get; private set; }

		Handler m_handler = new Handler(Looper.MainLooper);
		string m_name;
		IPAddress m_ip;
		string m_inputEventPath;
		Socket m_socket;
		byte[] m_buffer = new byte[1400];

		public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId) => StartCommandResult.Sticky;
		public override IBinder OnBind(Intent intent) => null;

		public override void OnCreate()
		{
			base.OnCreate();
			IsRunning = true;
			if (MainActivity.Instance != null) {
				MainActivity.Instance.SetServiceRunning(true);
			}

			try {
				var settingPath = Path.Combine(GetExternalFilesDir(null).AbsolutePath, "settings.xml");
				var document = XDocument.Load(settingPath);
				var element = document.Root;
				m_name = element.Attribute("Name").Value;
				m_ip = IPAddress.Parse(element.Attribute("IP").Value);
				m_inputEventPath = element.Attribute("InputEventPath").Value;
			}
			catch {
				StopSelf();
				return;
			}

			InitTouchInjector();
			m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			m_socket.BeginConnect(m_ip, ServerPort, ServerConnectedCallback, m_socket);

			var proc = Java.Lang.Runtime.GetRuntime().Exec("su");
			var output = new DataOutputStream(proc.OutputStream);
			output.WriteBytes($"chmod 666 {m_inputEventPath}\n");
			output.Flush();
			output.WriteBytes("exit\n");
			output.Flush();
			proc.WaitFor();
			if (!OpenTouchDevice(m_inputEventPath)) {
				Toast.MakeText(this, "Unable to open input event ¨r(¨s¨Œ¨t)¨q\nPlease make sure the device is rooted.", ToastLength.Long).Show();
				StopSelf();
			}
		}

		public override void OnDestroy()
		{
			m_socket.Dispose();
			m_socket = null;
			CloseTouchInjector();

			var proc = Java.Lang.Runtime.GetRuntime().Exec("su");
			var output = new DataOutputStream(proc.OutputStream);
			output.WriteBytes($"chmod 660 {m_inputEventPath}\n");
			output.Flush();
			output.WriteBytes("exit\n");
			output.Flush();
			proc.WaitFor();

			IsRunning = false;
			if (MainActivity.Instance != null) {
				MainActivity.Instance.SetServiceRunning(false);
			}
			base.OnDestroy();
		}

		void ServerConnectedCallback(IAsyncResult ar)
		{
			var socket = (Socket)ar.AsyncState;

			try {
				socket.EndConnect(ar);
			}
			catch (SocketException) {
				m_handler.Post(StopSelf);
				return;
			}
			catch (ObjectDisposedException) {
				return;
			}

			socket.Send(Encoding.UTF8.GetBytes(m_name));
			socket.BeginReceive(m_buffer, 0, m_buffer.Length, SocketFlags.None, DataReceivedCallback, socket);
		}

		void DataReceivedCallback(IAsyncResult ar)
		{
			var socket = (Socket)ar.AsyncState;

			int bytesReceived;
			try {
				bytesReceived = socket.EndReceive(ar);
			}
			catch (SocketException) {
				m_handler.Post(StopSelf);
				return;
			}
			catch (ObjectDisposedException) {
				return;
			}
			if (bytesReceived == 0) {
				m_handler.Post(StopSelf);
				return;
			}

			var data = new byte[bytesReceived];
			Array.Copy(m_buffer, data, bytesReceived);
			ProcessReceivedData(data);
			socket.BeginReceive(m_buffer, 0, m_buffer.Length, SocketFlags.None, DataReceivedCallback, socket);
		}

		byte[] m_receivedData = new byte[6];
		int m_dataIndex = 0;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		struct InputEvent
		{
			public short type;
			public short code;
			public short value;
		}

		List<InputEvent> m_pendingEvents = new List<InputEvent>();

		void ProcessReceivedData(byte[] data)
		{
			foreach (var item in data) {
				m_receivedData[m_dataIndex++] = item;
				if (m_dataIndex == 6) {
					m_dataIndex = 0;
					m_pendingEvents.Add(new InputEvent {
						type = (short)(m_receivedData[0] | m_receivedData[1] << 8),
						code = (short)(m_receivedData[2] | m_receivedData[3] << 8),
						value = (short)(m_receivedData[4] | m_receivedData[5] << 8),
					});
				}
			}

			if (m_pendingEvents.Count > 0) {
				InjectTouchEvent(m_pendingEvents.ToArray(), m_pendingEvents.Count);
				m_pendingEvents.Clear();
			}
		}

		[DllImport("TouchInjector", EntryPoint = "TouchInjector_Init")]
		static extern void InitTouchInjector();

		[DllImport("TouchInjector", EntryPoint = "TouchInjector_OpenDevice")]
		static extern bool OpenTouchDevice([MarshalAs(UnmanagedType.LPStr)] string deviceName);

		[DllImport("TouchInjector", EntryPoint = "TouchInjector_Close")]
		static extern void CloseTouchInjector();

		[DllImport("TouchInjector", EntryPoint = "TouchInjector_InjectEvent")]
		static extern void InjectTouchEvent([MarshalAs(UnmanagedType.LPArray)] InputEvent[] inputEvents, int count);
	}
}
