using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SocketTemplate
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main(string[] args)
        {
            //var service = new SocketAsyncEventService("127.0.0.1", 12345);
            //service.Start();

            var service = new SocketDemo(100,50000);
            service.Init();
            service.Start(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345));

            while (true)
            {
                Console.ReadKey();
            }
        }
    }
}
