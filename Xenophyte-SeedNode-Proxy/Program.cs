using System;
using System.Collections.Generic;
using System.Threading;
using Xenophyte_SeedNode_Proxy.Automation;
using Xenophyte_SeedNode_Proxy.Log.Function;
using Xenophyte_SeedNode_Proxy.Setting.Function;
using Xenophyte_SeedNode_Proxy.TCP.Server;

namespace Xenophyte_SeedNode_Proxy
{
    public class Program
    {
        private static LogSystem _logSystem;
        private static ProxyFunction _proxyFunction;
        private static List<ProxyServerListener> _proxyServerListener;

        private static CommandLineSystem _commandLineSystem;
        private static CancellationTokenSource _cancellationSeedNodeProxyServer;

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the " + AppDomain.CurrentDomain.FriendlyName);


            _proxyFunction = new ProxyFunction();
            _proxyServerListener = new List<ProxyServerListener>();
            bool initProxy = _proxyFunction.LoadProxySetting() ? true : _proxyFunction.InitializeProxySetting();

            if (initProxy)
            {
                _cancellationSeedNodeProxyServer = new CancellationTokenSource();

                #region Initialize log system.

                _logSystem = new LogSystem(_proxyFunction.ProxySetting, _cancellationSeedNodeProxyServer);
                _logSystem.EnableWriteLog();

                #endregion

                foreach(int port in _proxyFunction.ProxySetting.ServerPort)
                {
                    _proxyServerListener.Add(new ProxyServerListener(_proxyFunction.ProxySetting, port,  _logSystem, _cancellationSeedNodeProxyServer));
                    if (!_proxyServerListener[_proxyServerListener.Count-1].StartServer())
                    {
                        Console.WriteLine("Failed to start the proxy server at port: " + port + ", please try again later.");
                        return;
                    }
                }

               

               
                Console.WriteLine(AppDomain.CurrentDomain.FriendlyName + " started successfully.");
                _commandLineSystem = new CommandLineSystem(
                    new AutomationSystem(_proxyServerListener),
                    _proxyFunction.ProxySetting,
                    _logSystem);
                _commandLineSystem.StartCommandLineSystem();
            }
            else
            {
                Console.WriteLine("Please, press a key to exit.");
                Console.ReadLine();
            }
        }
    }
}
