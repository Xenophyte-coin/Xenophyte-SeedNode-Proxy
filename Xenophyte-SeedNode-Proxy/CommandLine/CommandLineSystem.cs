using System;
using System.Net;
using System.Threading;
using Xenophyte_SeedNode_Proxy.Automation;
using Xenophyte_SeedNode_Proxy.CommandLine.Enum;
using Xenophyte_SeedNode_Proxy.Log.Enum;
using Xenophyte_SeedNode_Proxy.Log.Function;
using Xenophyte_SeedNode_Proxy.Setting.Object;
using Xenophyte_SeedNode_Proxy.TCP.Enum;

namespace Xenophyte_SeedNode_Proxy
{
    public class CommandLineSystem
    {
        private AutomationSystem _automationSystem;
        private ProxySetting _proxySetting;
        private LogSystem _logSystem;
        private bool _proxyServerClosed;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="automationSystem"></param>
        /// <param name="proxySetting"></param>
        /// <param name="logSystem"></param>
        public CommandLineSystem(
            AutomationSystem automationSystem,
            ProxySetting proxySetting,
            LogSystem logSystem)
        {
            _automationSystem = automationSystem;
            _proxySetting = proxySetting;
            _logSystem = logSystem;
        }

        /// <summary>
        /// Start the command line system.
        /// </summary>
        public void StartCommandLineSystem()
        {
            new Thread(() =>
            {
                Console.WriteLine(AppDomain.CurrentDomain.FriendlyName + " has been started successfully.");
                Console.WriteLine("Write the command line: " + CommandLineEnum.Help + " to get informations.");

                while (!_proxyServerClosed)
                {
                    string commandLine = Console.ReadLine();

                    switch (commandLine)
                    {
                        case CommandLineEnum.Help:
                            {
                                _logSystem.WriteLine("<# List of command lines #>", LogEnum.GENERAL, ConsoleColor.White);
                                _logSystem.WriteLine(CommandLineEnum.Help + " - Show every command lines.", LogEnum.GENERAL, ConsoleColor.White);
                                _logSystem.WriteLine(CommandLineEnum.AddHost + " - Permit to insert a new Seed Node Host.", LogEnum.GENERAL, ConsoleColor.White);
                                _logSystem.WriteLine(CommandLineEnum.Stats + " - Show stats of the proxy server.", LogEnum.GENERAL, ConsoleColor.White);
                                _logSystem.WriteLine(CommandLineEnum.Exit + " - Close the proxy server.", LogEnum.GENERAL, ConsoleColor.White);
                            }
                            break;
                        case CommandLineEnum.AddHost:
                            {
                                string host = Console.ReadLine();

                                if (!IPAddress.TryParse(host, out _))
                                    _logSystem.WriteLine("Failed to insert " + host + ", the IP address is invalid.", LogEnum.GENERAL, ConsoleColor.Red);
                                else if (_proxySetting.SeedNodeHostList.Contains(host))
                                    _logSystem.WriteLine("Failed to insert " + host + ", the IP address is already inserted.", LogEnum.GENERAL, ConsoleColor.Yellow);
                                else if (_proxySetting.ServerIp == host)
                                    _logSystem.WriteLine("Failed to insert " + host +", the IP address is the same of the proxy server.", LogEnum.GENERAL, ConsoleColor.Yellow);
                                else
                                    _proxySetting.SeedNodeHostList.Add(host);
                            }
                            break;
                        case CommandLineEnum.Stats:
                            {

                                _logSystem.WriteLine("########## ONLINE MODE #########", LogEnum.GENERAL, ConsoleColor.Cyan);
                                _logSystem.WriteLine("Count proxy client: " + _automationSystem.GetTotalOnlineNetworkConnection(ProxyServerEnumStats.COUNT_ALL), LogEnum.GENERAL, ConsoleColor.Magenta);
                                _logSystem.WriteLine("Count proxy client alive: " + _automationSystem.GetTotalOnlineNetworkConnection(ProxyServerEnumStats.COUNT_ALIVE), LogEnum.GENERAL, ConsoleColor.Green);
                                _logSystem.WriteLine("Count proxy client dead: " + _automationSystem.GetTotalOnlineNetworkConnection(ProxyServerEnumStats.COUNT_DEAD), LogEnum.GENERAL, ConsoleColor.Red);
                                _logSystem.WriteLine("################################", LogEnum.GENERAL, ConsoleColor.Gray);

                                _logSystem.WriteLine("########## REMOTE HTTP MODE #########", LogEnum.GENERAL, ConsoleColor.Cyan);
                                _logSystem.WriteLine("Count proxy client: " + _automationSystem.GetTotalRemoteHttpNetworkConnection(ProxyServerEnumStats.COUNT_ALL), LogEnum.GENERAL, ConsoleColor.Magenta);
                                _logSystem.WriteLine("Count proxy client alive: " + _automationSystem.GetTotalRemoteHttpNetworkConnection(ProxyServerEnumStats.COUNT_ALIVE), LogEnum.GENERAL, ConsoleColor.Green);
                                _logSystem.WriteLine("Count proxy client dead: " + _automationSystem.GetTotalRemoteHttpNetworkConnection(ProxyServerEnumStats.COUNT_DEAD), LogEnum.GENERAL, ConsoleColor.Red);
                                _logSystem.WriteLine("################################", LogEnum.GENERAL, ConsoleColor.Gray);



                                _logSystem.WriteLine("########## REMOTE MODE #########", LogEnum.GENERAL, ConsoleColor.Cyan);
                                _logSystem.WriteLine("Count proxy client: " + _automationSystem.GetTotalRemoteNetworkConnection(ProxyServerEnumStats.COUNT_ALL), LogEnum.GENERAL, ConsoleColor.Magenta);
                                _logSystem.WriteLine("Count proxy client alive: " + _automationSystem.GetTotalRemoteNetworkConnection(ProxyServerEnumStats.COUNT_ALIVE), LogEnum.GENERAL, ConsoleColor.Green);
                                _logSystem.WriteLine("Count proxy client dead: " + _automationSystem.GetTotalRemoteNetworkConnection(ProxyServerEnumStats.COUNT_DEAD), LogEnum.GENERAL, ConsoleColor.Red);
                                _logSystem.WriteLine("################################", LogEnum.GENERAL, ConsoleColor.Gray);


                                _logSystem.WriteLine("########## TOKEN MODE #########", LogEnum.GENERAL, ConsoleColor.Cyan);
                                _logSystem.WriteLine("Count proxy client: " + _automationSystem.GetTotalTokenNetworkConnection(ProxyServerEnumStats.COUNT_ALL), LogEnum.GENERAL, ConsoleColor.Magenta);
                                _logSystem.WriteLine("Count proxy client alive: " + _automationSystem.GetTotalTokenNetworkConnection(ProxyServerEnumStats.COUNT_ALIVE), LogEnum.GENERAL, ConsoleColor.Green);
                                _logSystem.WriteLine("Count proxy client dead: " + _automationSystem.GetTotalTokenNetworkConnection(ProxyServerEnumStats.COUNT_DEAD), LogEnum.GENERAL, ConsoleColor.Red);
                                _logSystem.WriteLine("################################", LogEnum.GENERAL, ConsoleColor.Gray);

                            }
                            break;
                        case CommandLineEnum.Exit:
                            {
                                _logSystem.WriteLine("Closing the SeedNode Proxy Server..", LogEnum.GENERAL, ConsoleColor.White);
                                if (_automationSystem.StopProxyServer())
                                {
                                    _logSystem.WriteLine("The proxy server listener has been closed succesfully.", LogEnum.GENERAL, ConsoleColor.Green);
                                    _proxyServerClosed = true;
                                }
                                else _logSystem.WriteLine("Failed to close the proxy server listener..", LogEnum.GENERAL, ConsoleColor.Red);
                            }
                            break;
                    }
                }

                _logSystem.WriteLine("The seed node proxy server has been closed, successfully.", LogEnum.GENERAL, ConsoleColor.White);
                _logSystem.WriteLine("Please, press a key to exit.", LogEnum.GENERAL, ConsoleColor.White);
                Console.ReadLine();

            }).Start();
        }
    }
}
