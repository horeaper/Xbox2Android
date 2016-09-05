using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.IO;

namespace Xbox2AndroidClient
{
	[Service]
	public partial class NetworkService : Service
	{
		public static bool IsRunning { get; private set; }

		Handler m_handler = new Handler(Looper.MainLooper);
		Socket m_socket;
		byte[] m_buffer = new byte[1400];
		Java.Lang.Process m_proc;
		DataOutputStream m_output;

		public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId) => StartCommandResult.Sticky;
		public override IBinder OnBind(Intent intent) => null;

		public override void OnCreate()
		{
			base.OnCreate();
			IsRunning = true;

			m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			m_socket.BeginConnect(MainActivity.IP,  MainActivity.ServerPort, ServerConnectedCallback, m_socket);
			m_proc = Java.Lang.Runtime.GetRuntime().Exec("su");
			m_output = new DataOutputStream(m_proc.OutputStream);
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			m_socket.Dispose();
			m_socket = null;
			m_output.WriteBytes("exit\n");
			m_output.Flush();
			IsRunning = false;
			if (MainActivity.Instance != null) {
				MainActivity.Instance.SetServiceRunning(false);
			}
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

			socket.Send(Encoding.UTF8.GetBytes(MainActivity.Name));
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
			m_handler.Post(() => ProcessReceivedData(data));
			socket.BeginReceive(m_buffer, 0, m_buffer.Length, SocketFlags.None, DataReceivedCallback, socket);
		}

		StringBuilder m_sendString = new StringBuilder();

		void ProcessReceivedData(byte[] data)
		{
			foreach (var item in data) {
				if (item != '\n') {
					m_sendString.Append((char)item);
				}
				else {
					var text = m_sendString.ToString();

					m_sendString.Clear();
				}
			}
		}
	}
}
