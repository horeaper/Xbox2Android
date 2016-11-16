using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Controls;

namespace Xbox2Android
{
	partial class MainWindow
	{
		public const short ListenPort = 21499;

		public class ClientParam
		{
			public string Name;
			public int Type;
			public float Width;
			public float Height;

			public Socket Socket;
			public byte[] Buffer = new byte[1400];

			public readonly List<byte> ReceivedData = new List<byte>();
		}

		public void StartServer()
		{
			var addresses = Dns.GetHostAddresses(Dns.GetHostName());
			var ipList = addresses.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
			var displayList = new List<IPAddress>();
			foreach (var ip in ipList) {
				var bytes = ip.GetAddressBytes();
				if (bytes[0] != 169 && bytes[1] != 254) {
					displayList.Add(ip);

					var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					listener.Bind(new IPEndPoint(ip, ListenPort));
					listener.Listen(10);
					listener.BeginAccept(ClientConnectedCallback, listener);
				}
			}
			displayList.Sort((left, right) => {
				var bytesL = left.GetAddressBytes();
				var bytesR = right.GetAddressBytes();
				int valL = bytesL[0] << 24 | bytesL[1] << 16 | bytesL[2] << 8 | bytesL[3];
				int valR = bytesR[0] << 24 | bytesR[1] << 16 | bytesR[2] << 8 | bytesR[3];
				if (valL < valR) {
					return -1;
				}
				else if (valL > valR) {
					return 1;
				}
				else {
					return 0;
				}
			});
			foreach (var ip in displayList) {
				menuLocalIPs.Items.Add(new MenuItem {
					Header = ip.ToString()
				});
			}
		}

		public void RemoveClient(ClientParam client)
		{
			Dispatcher.Invoke(() => {
				foreach (ListBoxItem item in listClients.Items) {
					if (ReferenceEquals(item.Tag, client)) {
						listClients.Items.Remove(item);
						if (listClients.SelectedIndex == -1 && listClients.Items.Count > 0) {
							listClients.SelectedIndex = 0;
						}
						return;
					}
				}
			});
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

		void ValidateDataAndRegister(ClientParam client)
		{
			if (client.Name == null) {
				int dataLength = client.ReceivedData[0];
				if (client.ReceivedData.Count >= dataLength + 1) {
					var reader = new BinaryReader(new MemoryStream(client.ReceivedData.ToArray()), Encoding.UTF8);
					reader.ReadByte();
					client.Name = reader.ReadString();
					client.Type = reader.ReadByte();
					client.Width = reader.ReadInt32();
					client.Height = reader.ReadInt32();
					client.ReceivedData.Clear();

					Dispatcher.Invoke(() => {
						listClients.Items.Add(new ListBoxItem {
							Content = client.Name,
							Tag = client,
						});
						if (listClients.SelectedIndex == -1) {
							listClients.SelectedIndex = 0;
						}
					});
				}
			}

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

			var data = new byte[bytesReceived];
			Array.Copy(client.Buffer, data, bytesReceived);
			client.ReceivedData.AddRange(data);
			ValidateDataAndRegister(client);

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

			var data = new byte[bytesReceived];
			Array.Copy(client.Buffer, data, bytesReceived);
			client.ReceivedData.AddRange(data);
			ValidateDataAndRegister(client);

			client.Socket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None, ReceiveNextDataCallback, client);
		}

		public void SendData(byte[] data)
		{
			m_selectedClient?.Socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendDataCallback, m_selectedClient);
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
