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
        private IPEndPoint _LocalEndPoint = null;
        private ConcurrentDictionary<string, AsyncUserToken> _UserTokens = new ConcurrentDictionary<string, AsyncUserToken>();

        #region Constructor
        public SocketService(string ip, int port)
        {
            _LocalEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }
        #endregion

        #region Public Methods
        public void StartListen()
        {
            try
            {
                using (var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    listenSocket.Bind(_LocalEndPoint);
                    listenSocket.Listen(1000);
                    Console.WriteLine($"开始监听{_LocalEndPoint.Address}:{_LocalEndPoint.Port}");
                    StartAccept(listenSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StartListen错误：{ex.Message}");
            }
        }
        #endregion

        #region Private Methods
        private void StartAccept(Socket listenSocket)
        {
            var clientSocket = listenSocket.Accept();
            var ipEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;
            var id = $"{ipEndPoint.Address.ToString()}:{ipEndPoint.Port}";
            Console.WriteLine($"{id}已连接");

            var userToken = new AsyncUserToken
            {
                ID = id,
                ConnectionTime = DateTime.Now,
                IPEndPoint = ipEndPoint,
                Socket = clientSocket
            };
            _UserTokens.TryAdd(id, userToken);

            clientSocket.BeginReceive(userToken.Buffer, 0, userToken.Buffer.Length, SocketFlags.None, ReceiveCallback, userToken);

            StartAccept(listenSocket);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            var connection = (AsyncUserToken)ar.AsyncState;
            try
            {
                int length = connection.Socket.EndReceive(ar);
                if (length == 0)
                {
                    RemoveSocket(connection.ID);
                    return;
                }

                var msg = Encoding.GetEncoding("GB2312").GetString(connection.Buffer, 0, length);
                var remoteEndPoint = (IPEndPoint)connection.Socket.RemoteEndPoint;
                Console.WriteLine($"{remoteEndPoint.Address}:{remoteEndPoint.Port}消息：{msg}");
                connection.Socket.Send(Encoding.GetEncoding("GB2312").GetBytes(msg));
                connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, ReceiveCallback, connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReceiveCallback错误：{ex.Message}");
                RemoveSocket(connection.ID);
            }
        }

        private void RemoveSocket(string id)
        {
            if (_UserTokens.TryRemove(id, out AsyncUserToken remove))
            {
                remove.Close();
            }
        }
        #endregion

    }
}
