using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketTemplate.Models
{
    public class AsyncUserToken
    {
        public const int BufferSize = 10 * 1024;

        public byte[] Buffer = new byte[BufferSize];

        public string ID { get; set; }

        public IPEndPoint IPEndPoint { get; set; }

        public DateTime ConnectionTime { get; set; }

        public Socket Socket { get; set; }

        public void Close()
        {
            if (Socket == null)
            {
                return;
            }

            Socket.Shutdown(SocketShutdown.Both);
            Socket.Dispose();
        }
    }
}
