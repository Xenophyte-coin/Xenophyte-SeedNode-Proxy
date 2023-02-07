using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xenophyte_Connector_All.Setting;
using Xenophyte_SeedNode_Proxy.Log.Enum;
using Xenophyte_SeedNode_Proxy.Log.Function;
using Xenophyte_SeedNode_Proxy.Setting.Object;
using Xenophyte_SeedNode_Proxy.TCP.Client;
using Xenophyte_SeedNode_Proxy.TCP.Enum;

namespace Xenophyte_SeedNode_Proxy.TCP.Server
{
    public class ProxyServerListener
    {

        /// <summary>
        /// Server status & settings.
        /// </summary>
        private bool _serverIsClosed;
        private ProxySetting _proxySetting;
        private int _serverPort;
        private LogSystem _logSystem;
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// TcpListener.
        /// </summary>
        private TcpListener _serverListener;
        private ConcurrentDictionary<string, List<ProxyClient>> _listProxyClient;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="serverIp"></param>
        /// <param name="serverPort"></param>
        public ProxyServerListener(ProxySetting proxySetting, int idPort, LogSystem logSystem, CancellationTokenSource cancellation)
        {
            _proxySetting = proxySetting;
            _serverPort = idPort;
            _listProxyClient = new ConcurrentDictionary<string, List<ProxyClient>>();
            _logSystem = logSystem;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
        }

        #region Start/Stop the Proxy Seed Node Server.

        /// <summary>
        /// Start the proxy server listener.
        /// </summary>
        /// <returns></returns>
        public bool StartServer()
        {
            try
            {
                _serverListener = new TcpListener(IPAddress.Parse(_proxySetting.ServerIp), _serverPort);
                _serverListener.Start();

                _logSystem.WriteLine(_proxySetting.ServerIp + ":" + _serverPort + " server listener started successfully", LogEnum.SERVER, ConsoleColor.Cyan);

                Task.Factory.StartNew(async () =>
                {

                    while (!_serverIsClosed)
                    {
                        string clientIp = string.Empty;

                        try
                        {
                            Socket tcpClient = await _serverListener.AcceptSocketAsync();

                            LogEnum clientEnum = LogEnum.GENERAL;
                            LogEnum seedEnum = LogEnum.GENERAL;

                            switch (_serverPort)
                            {
                                case ClassConnectorSetting.SeedNodePort:
                                    {
                                        clientEnum = LogEnum.CLIENT_ONLINE;
                                        seedEnum = LogEnum.SEED_ONLINE;
                                    }
                                    break;
                                case ClassConnectorSetting.RemoteNodePort:
                                    {
                                        clientEnum = LogEnum.CLIENT_REMOTE;
                                        seedEnum = LogEnum.SEED_REMOTE;
                                    }
                                    break;
                                case ClassConnectorSetting.SeedNodeTokenPort:
                                    {
                                        clientEnum = LogEnum.CLIENT_TOKEN;
                                        seedEnum = LogEnum.SEED_TOKEN;
                                    }
                                    break;
                            }

                            await Task.Factory.StartNew(async () =>
                            {
                                #region Initialize the proxy client or close it.

                                ProxyClient proxyClient = new ProxyClient(
                                    tcpClient,
                                    _logSystem,
                                    _proxySetting,
                                    _serverPort,
                                    clientEnum,
                                    seedEnum,
                                    _cancellationTokenSource);

                                clientIp = proxyClient.GetProxyClientIp();

                                #endregion

                                #region Insert the proxy client initialized or close it.

                                if (!_listProxyClient.ContainsKey(clientIp))
                                {
                                    if (!_listProxyClient.TryAdd(clientIp, new List<ProxyClient>()))
                                        proxyClient.CloseProxyClient(false);
                                }


                                if (await proxyClient.HandleProxyClient())
                                    _listProxyClient[clientIp].Add(proxyClient);



                                #endregion
                            }).ConfigureAwait(false);
                        }

                        catch (Exception error)
                        {
                            _logSystem.WriteLine("Can't handle a new incoming client to the proxy " + clientIp + ". | Error: " + error.Message, LogEnum.SERVER, ConsoleColor.Red);
                        }
                    }
                
                }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch(Exception error)
            {
                _logSystem.WriteLine("Failed to start the proxy server listener. Exception: " + error.Message, LogEnum.SERVER, ConsoleColor.Red);
                return false;
            }

            _logSystem.WriteLine("Successfully to start the proxy server listener at "+_proxySetting.ServerIp+":"+_serverPort, LogEnum.SERVER, ConsoleColor.Magenta);


            return true;
        }

        /// <summary>
        /// Close the proxy server listener.
        /// </summary>
        /// <returns></returns>
        public bool StopServer()
        {
            try
            {
                _serverIsClosed = true;
                _serverListener.Stop();
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _cancellationTokenSource.Cancel();

                ClearProxyClientListing(true);
                _logSystem.WriteLine("The proxy server has been closed successfully.", LogEnum.SERVER, ConsoleColor.Green);
            }
            catch(Exception error)
            {
                _logSystem.WriteLine("Failed to stop the proxy server. Exception: " + error.Message, LogEnum.SERVER, ConsoleColor.Red);
                return true;
            }
            return true;
        }

        #endregion


        #region Manage the proxy seed node server.

        /// <summary>
        /// Close proxy client listing.
        /// </summary>
        /// <param name="all"></param>
        public void ClearProxyClientListing(bool all)
        {
            long totalClosed = 0;
            foreach(string clientIp in _listProxyClient.Keys.ToArray())
            {
                for(int i = 0; i < _listProxyClient[clientIp].Count; i++)
                {
                    if (i < _listProxyClient[clientIp].Count)
                    {
                        try
                        {
                            if (!_listProxyClient[clientIp][i].ProxyClientStatus || all)
                            {
                                _listProxyClient[clientIp][i].CloseProxyClient(false);
                                totalClosed++;
                            }
                        }
                        catch
                        {
                            // Ignored.
                        }
                    }
                }
            }
            _logSystem.WriteLine("Total proxy client closed successfully: " + totalClosed, LogEnum.SERVER, ConsoleColor.Green);
        }

        #endregion

        #region Statistics about the Proxy Seed Node Server.

        /// <summary>
        /// Get the amount of proxy client stats depending of the Enumeration provided.
        /// </summary>
        /// <param name="proxyServerEnumStats"></param>
        /// <returns></returns>
        public long GetProxyClientCount(ProxyServerEnumStats proxyServerEnumStats)
        {
            long total = 0;

            foreach (string clientIp in _listProxyClient.Keys.ToArray())
            {
                if (proxyServerEnumStats == ProxyServerEnumStats.COUNT_ALL)
                    total += _listProxyClient[clientIp].Count;
                else
                {
                    for (int i = 0; i < _listProxyClient[clientIp].Count; i++)
                    {
                        if (i < _listProxyClient[clientIp].Count)
                        {
                            switch(proxyServerEnumStats)
                            {
                                case ProxyServerEnumStats.COUNT_ALIVE:
                                    {
                                        if (_listProxyClient[clientIp][i].ProxyClientStatus)
                                            total++;
                                    }
                                    break;
                                case ProxyServerEnumStats.COUNT_DEAD:
                                    {
                                        if (!_listProxyClient[clientIp][i].ProxyClientStatus)
                                            total++;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            return total;
        }

        #endregion
    }
}
