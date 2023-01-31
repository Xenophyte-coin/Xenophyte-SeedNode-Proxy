using System;
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
        private static ProxyServerListener _proxyServerOnlineListener;
        private static ProxyServerListener _proxyServerRemoteListener;
        private static ProxyServerListener _proxyServerTokenListener;
        private static CommandLineSystem _commandLineSystem;
        private static CancellationTokenSource _cancellationSeedNodeProxyServer;

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the " + AppDomain.CurrentDomain.FriendlyName);


            _proxyFunction = new ProxyFunction();
            bool initProxy = _proxyFunction.LoadProxySetting() ? true : _proxyFunction.InitializeProxySetting();

            if (initProxy)
            {
                _cancellationSeedNodeProxyServer = new CancellationTokenSource();

                #region Initialize log system.

                _logSystem = new LogSystem(_proxyFunction.ProxySetting, _cancellationSeedNodeProxyServer);
                _logSystem.EnableWriteLog();

                #endregion

                _proxyServerOnlineListener = new ProxyServerListener(_proxyFunction.ProxySetting, _proxyFunction.ProxySetting.ServerPort[0], _logSystem, _cancellationSeedNodeProxyServer);
                if (!_proxyServerOnlineListener.StartServer())
                {
                    Console.WriteLine("Failed to start the proxy server at port: " + _proxyFunction.ProxySetting.ServerPort[0] + ", please try again later.");
                    return;
                }

                _proxyServerRemoteListener = new ProxyServerListener(_proxyFunction.ProxySetting, _proxyFunction.ProxySetting.ServerPort[1], _logSystem, _cancellationSeedNodeProxyServer);
                if (!_proxyServerRemoteListener.StartServer())
                {
                    Console.WriteLine("Failed to start the proxy server: " + _proxyFunction.ProxySetting.ServerPort[1] + ", please try again later.");
                    return;
                }

                _proxyServerTokenListener = new ProxyServerListener(_proxyFunction.ProxySetting, _proxyFunction.ProxySetting.ServerPort[2], _logSystem, _cancellationSeedNodeProxyServer);
                if (!_proxyServerTokenListener.StartServer())
                {
                    Console.WriteLine("Failed to start the proxy server: " + _proxyFunction.ProxySetting.ServerPort[2] + ", please try again later.");
                    return;
                }

                Console.WriteLine(AppDomain.CurrentDomain.FriendlyName + " started successfully.");
                _commandLineSystem = new CommandLineSystem(
                    new AutomationSystem(_proxyServerOnlineListener, _proxyServerRemoteListener, _proxyServerTokenListener),
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
