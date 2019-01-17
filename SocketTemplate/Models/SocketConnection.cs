using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketTemplate.Models
{
    public class SocketConnection
    {
        public const int BufferSize = 10 * 1024;

        public byte[] Buffer = new byte[BufferSize];

        public string ID { get; set; }

        public DateTime ConnectionTime { get; set; }

        public Socket ClientSocket { get; set; }

        public void Close()
        {
            if (ClientSocket == null)
            {
                return;
            }

            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Dispose();
        }
    }
}
