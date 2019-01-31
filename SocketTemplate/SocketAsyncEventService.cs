using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketTemplate.Models;

namespace SocketTemplate
{
    public class SocketAsyncEventService : ISocketService
    {
        private readonly IPEndPoint _LocalEndPoint = null;
        private const int _MaxConnections = 5;//同时的连接数
        private const int _BufferSize = 500;
        private const int _OpsToPreAlloc = 2;//read, write
        private const int _ListenerBacklog = 1000;
        private Socket _Listener = null;
        private readonly static Encoding _Encoding = Encoding.GetEncoding("GB2312");
        private SocketAsyncEventArgsPool _SocketAsyncEventArgsPool = new SocketAsyncEventArgsPool(_MaxConnections);
        private readonly Semaphore _Semaphore = new Semaphore(_MaxConnections, _MaxConnections);
        private ConcurrentDictionary<string, AsyncUserToken> _UserTokens = new ConcurrentDictionary<string, AsyncUserToken>();

        #region Constructor
        public SocketAsyncEventService(string ip, int port)
        {
            _LocalEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }
        #endregion

        #region Public Methods
        public void Start()
        {
            try
            {
                Init();
                _Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _Listener.Bind(_LocalEndPoint);
                _Listener.Listen(_ListenerBacklog);
                Console.WriteLine($"开始监听{_LocalEndPoint.Address}:{_LocalEndPoint.Port}");
                StartAccept(null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Start错误：{ex.Message}");
            }
        }
        #endregion

        #region Private Methods
        //给缓冲池分配内存，并分配给SocketAsyncEventArg
        private void Init()
        {
            var bufferManager = new BufferManager(_BufferSize * _MaxConnections * _OpsToPreAlloc, _BufferSize);
            bufferManager.InitBuffer();
            for (int i = 0; i < _MaxConnections; i++)
            {
                var socketAsyncEventArgs = new SocketAsyncEventArgs();
                socketAsyncEventArgs.Completed += IO_Completed;

                //将缓冲池中的字节缓冲区分配给SocketAsyncEventArg对象
                bufferManager.SetBuffer(socketAsyncEventArgs);

                _SocketAsyncEventArgsPool.Push(socketAsyncEventArgs);
            }
        }

        //完成接收或发送操作后调用
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("套接字的最后操作不是接收或发送");
            }
        }

        private void StartAccept(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            if (socketAsyncEventArgs == null)
            {
                socketAsyncEventArgs = new SocketAsyncEventArgs();
                socketAsyncEventArgs.Completed += ProcessAccept;
            }
            else
            {
                socketAsyncEventArgs.AcceptSocket = null;
            }

            _Semaphore.WaitOne();
            if (!_Listener.AcceptAsync(socketAsyncEventArgs))
            {
                ProcessAccept(null, socketAsyncEventArgs);
            }
        }

        private void ProcessAccept(object sender, SocketAsyncEventArgs e)
        {
            var readEventArgs = _SocketAsyncEventArgsPool.Pop();
            var ipEndPoint = (IPEndPoint)e.AcceptSocket.RemoteEndPoint;
            var id = $"{ipEndPoint.Address.ToString()}:{ipEndPoint.Port}";
            Console.WriteLine($"{id}已连接");

            var userToken = new AsyncUserToken
            {
                ID = id,
                ConnectionTime = DateTime.Now,
                IPEndPoint = ipEndPoint,
                Socket = e.AcceptSocket
            };
            readEventArgs.UserToken = userToken;
            _UserTokens.TryAdd(id, userToken);

            if (!e.AcceptSocket.ReceiveAsync(readEventArgs))
            {
                ProcessReceive(readEventArgs);
            }

            StartAccept(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred == 0 || e.SocketError != SocketError.Success)
                {
                    RemoveSocket(e);
                    return;
                }

                var userToken = (AsyncUserToken)e.UserToken;
                var msg = _Encoding.GetString(e.Buffer);
                Console.WriteLine($"{userToken.ID}：{msg}");

                e.SetBuffer(e.Offset, e.BytesTransferred);
                if (!userToken.Socket.SendAsync(e))
                {
                    ProcessSend(e);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProcessReceive：{ex.Message}");
                RemoveSocket(e);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            var userToken = (AsyncUserToken)e.UserToken;
            if (e.SocketError != SocketError.Success)
            {
                RemoveSocket(e);
            }

            if (!userToken.Socket.ReceiveAsync(e))
            {
                ProcessReceive(e);
            }
        }

        private void RemoveSocket(SocketAsyncEventArgs e)
        {
            var userToken = (AsyncUserToken)e.UserToken;
            if (_UserTokens.ContainsKey(userToken.ID))
            {
                _SocketAsyncEventArgsPool.Push(e);
                _Semaphore.Release();
                DisposeSocket(userToken.Socket);
                Console.WriteLine($"{userToken.ID}断开连接");
            }
        }

        private void DisposeSocket(Socket socket)
        {
            if (socket == null)
            {
                return;
            }

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket.Dispose();
        }
        #endregion


    }

}
