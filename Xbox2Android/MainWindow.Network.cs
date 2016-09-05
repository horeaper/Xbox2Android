using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Xbox2Android
{
	partial class MainWindow
	{
		public const short ListenPort = 21499;

		public class ClientParam
		{
			public Socket Socket;
			public byte[] Buffer = new byte[1400];
		}

		readonly List<ClientParam> m_clients = new List<ClientParam>();

		public void StartServer()
		{
			var addresses = Dns.GetHostAddresses(Dns.GetHostName());
			var ipList = addresses.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
			foreach (var ip in ipList) {
				var bytes = ip.GetAddressBytes();
				if (bytes[0] != 169 && bytes[1] != 254) {
					listLocalIPs.Items.Add(ip);

					var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					listener.Bind(new IPEndPoint(ip, ListenPort));
					listener.Listen(10);
					listener.BeginAccept(ClientConnectedCallback, listener);
				}
			}
		}

		public void RemoveClient(ClientParam client)
		{
			m_clients.Remove(client);

			foreach (ComboBoxItem item in comboDevices.Items) {
				if (ReferenceEquals(item.Tag, client)) {
					comboDevices.Items.Remove(item);
					return;
				}
			}
		}

		void ClientConnectedCallback(IAsyncResult ar)
		{
			var listener = (Socket)ar.AsyncState;

			try {
				var socket = listener.EndAccept(ar);
				var client = new ClientParam { Socket = socket };
				socket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None, ReceiveFirstDataCallback, client);
			}
			catch (SocketException) {
				listener.Dispose();
				return;
			}
			catch (ObjectDisposedException) {
				return;
			}

			listener.BeginAccept(ClientConnectedCallback, listener);
		}

		void ReceiveFirstDataCallback(IAsyncResult ar)
		{
			var client = (ClientParam)ar.AsyncState;

			int bytesReceived;
			try {
				bytesReceived = client.Socket.EndReceive(ar);
			}
			catch (SocketException) {
				client.Socket.Dispose();
				RemoveClient(client);
				return;
			}
			catch (ObjectDisposedException) {
				RemoveClient(client);
				return;
			}
			if (bytesReceived == 0) {
				client.Socket.Dispose();
				RemoveClient(client);
				return;
			}

			m_clients.Add(client);
			Dispatcher.Invoke(() => {
				comboDevices.Items.Add(new ComboBoxItem {
					Content = Encoding.UTF8.GetString(client.Buffer, 0, bytesReceived),
					Tag = client,
				});
				if (comboDevices.SelectedIndex == -1) {
					comboDevices.SelectedIndex = 0;
				}
			});

			client.Socket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None, ReceiveNextDataCallback, client);
		}

		void ReceiveNextDataCallback(IAsyncResult ar)
		{
			var client = (ClientParam)ar.AsyncState;

			int bytesReceived;
			try {
				bytesReceived = client.Socket.EndReceive(ar);
			}
			catch (SocketException) {
				client.Socket.Dispose();
				RemoveClient(client);
				return;
			}
			catch (ObjectDisposedException) {
				RemoveClient(client);
				return;
			}
			if (bytesReceived == 0) {
				client.Socket.Dispose();
				RemoveClient(client);
				return;
			}

			client.Socket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None, ReceiveNextDataCallback, client);
		}

		public void SendString(ClientParam client, string text)
		{
			var data = Encoding.ASCII.GetBytes(text);
			client.Socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendDataCallback, client);
		}

		void SendDataCallback(IAsyncResult ar)
		{
			var client = (ClientParam)ar.AsyncState;

			try {
				client.Socket.EndSend(ar);
			}
			catch (SocketException) {
				client.Socket.Dispose();
				RemoveClient(client);
			}
			catch (ObjectDisposedException) {
				RemoveClient(client);
			}
		}
	}
}
