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
        private const int _ListenerBacklog = 1000;
        private readonly static Encoding _Encoding = Encoding.GetEncoding("GB2312");
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
                using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(_LocalEndPoint);
                    listener.Listen(_ListenerBacklog);
                    Console.WriteLine($"开始监听{_LocalEndPoint.Address}:{_LocalEndPoint.Port}");
                    StartAccept(listener);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StartListen错误：{ex.Message}");
            }
        }
        #endregion

        #region Private Methods
        private void StartAccept(Socket listener)
        {
            var clientSocket = listener.Accept();
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

            StartAccept(listener);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            var userToken = (AsyncUserToken)ar.AsyncState;
            try
            {
                int length = userToken.Socket.EndReceive(ar);
                if (length == 0)
                {
                    RemoveSocket(userToken.ID);
                    return;
                }

                var msg = _Encoding.GetString(userToken.Buffer, 0, length);
                var remoteEndPoint = (IPEndPoint)userToken.Socket.RemoteEndPoint;
                Console.WriteLine($"{remoteEndPoint.Address}:{remoteEndPoint.Port}消息：{msg}");
                userToken.Socket.Send(_Encoding.GetBytes(msg));
                userToken.Socket.BeginReceive(userToken.Buffer, 0, userToken.Buffer.Length, SocketFlags.None, ReceiveCallback, userToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReceiveCallback错误：{ex.Message}");
                RemoveSocket(userToken.ID);
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
