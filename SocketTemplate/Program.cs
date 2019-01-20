using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
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
            if (args == null || args.Length != 1)
            {
                throw new Exception("命令行参数错误");
            }

            var arg = string.Concat(args);
            switch (arg)
            {
                case "-i":
                    ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                    break;
                case "-u":
                    ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                    break;
                case "-c":
                    RunConsole();
                    Console.ReadLine();
                    break;
            }
        }

        private static void RunConsole()
        {
            var service = new SocketService("127.0.0.1", 12345);
            service.StartListen();
            Console.ReadKey();
        }
    }
}
