using SocketTemplate.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketTemplate
{
    public class SocketService
    {
        private readonly object _StartLock = new object();
        private bool _IsStart = false;
        private static readonly IPAddress _Address = IPAddress.Parse("127.0.0.1");
        private static readonly int _Port = 12345;
        private ConcurrentDictionary<string, SocketConnection> _ClientSockets = new ConcurrentDictionary<string, SocketConnection>();

        #region Instance
        private static readonly Lazy<SocketService> _SocketServiceLazy = new Lazy<SocketService>(() => new SocketService());
        public static SocketService Instance
        {
            get
            {
                return _SocketServiceLazy.Value;
            }
        }
        #endregion

        #region Constructor
        private SocketService()
        {

        }
        #endregion

        public void Start()
        {
            if (_IsStart)
            {
                return;
            }
            lock (_StartLock)
            {
                if (_IsStart)
                {
                    return;
                }
                var thread = new Thread(BeginListening)
                {
                    IsBackground = true
                };
                thread.Start();
                _IsStart = true;
            }
        }

        private void BeginListening()
        {
            try
            {
                using (var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    var ipEndPoint = new IPEndPoint(_Address, _Port);
                    serverSocket.Bind(ipEndPoint);
                    serverSocket.Listen(1000);
                    Console.WriteLine($"{_Address}:{_Port} 服务端开始监听");

                    while (true)
                    {
                        var clientSocket = serverSocket.Accept();

                        var remoteEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
                        var id = $"{remoteEndPoint.Address.ToString()}:{remoteEndPoint.Port}";
                        Console.WriteLine($"{id} 已连接");

                        var connection = new SocketConnection
                        {
                            ID = id,
                            ConnectionTime = DateTime.Now,
                            ClientSocket = clientSocket
                        };
                        _ClientSockets.TryAdd(id, connection);

                        clientSocket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, ReceiveCallback, connection);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BeginListening错误：{ex.Message}");
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            var connection = (SocketConnection)ar.AsyncState;
            try
            {
                int length = connection.ClientSocket.EndReceive(ar);
                if (length == 0)
                {
                    RemoveSocket(connection.ID);
                    return;
                }

                var msg = Encoding.GetEncoding("GB2312").GetString(connection.Buffer, 0, length);
                Console.WriteLine($"收到消息：{msg}");
                connection.ClientSocket.Send(Encoding.GetEncoding("GB2312").GetBytes($"收到消息：{msg}"));
                connection.ClientSocket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, ReceiveCallback, connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReceiveCallback错误：{ex.Message}");
                RemoveSocket(connection.ID);
            }
        }

        private void RemoveSocket(string id)
        {
            if (_ClientSockets.TryRemove(id, out SocketConnection remove))
            {
                remove.Close();
            }
        }

    }
}
