using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xenophyte_Connector_All.Setting;
using Xenophyte_Connector_All.Utils;
using Xenophyte_SeedNode_Proxy.Log.Enum;
using Xenophyte_SeedNode_Proxy.Log.Function;
using Xenophyte_SeedNode_Proxy.Setting.Object;

namespace Xenophyte_SeedNode_Proxy.TCP.Client
{
    public class ProxyClient
    {
        /// <summary>
        /// Retrieve back the client status.
        /// </summary>
        public bool ProxyClientStatus { private set; get; }

        /// <summary>
        /// Seed node host target.
        /// </summary>
        public string SeedNodeHostTarget { private set; get; }

        /// <summary>
        /// Cancellation token source.
        /// </summary>
        private CancellationTokenSource _proxyClientTokenSource;

        /// <summary>
        /// TCP Client, of client and of seed node host target.
        /// </summary>
        private TcpClient _tcpProxyClient;
        private TcpClient _tcpSeedNodeClient;
        private int _seedNodePort;

        private LogSystem _logSystem;
        private ProxySetting _proxySetting;


        private LogEnum _clientEnum;
        private LogEnum _seedEnum;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="logSystem"></param>
        /// <param name="proxySetting"></param>
        /// <param name="seedNodePort"></param>
        /// <param name="clientEnum"></param>
        /// <param name="seedEnum"></param>
        /// <param name="cancellation"></param>
        public ProxyClient(TcpClient tcpClient, 
                            LogSystem logSystem, 
                            ProxySetting proxySetting, 
                            int seedNodePort,
                            LogEnum clientEnum,
                            LogEnum seedEnum,
                            CancellationTokenSource cancellation)
        {
            // Client Status.
            ProxyClientStatus = true;

            // TCP.
            _tcpProxyClient = tcpClient;
            _tcpSeedNodeClient = new TcpClient();
            _seedNodePort = seedNodePort;

            // Log.
            _logSystem = logSystem;

            // Setting.
            _proxySetting = proxySetting;

            // Enumeration type.
            _clientEnum = clientEnum;
            _seedEnum = seedEnum;

            // Cancellation.
            _proxyClientTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
        }


        /// <summary>
        /// Return proxy client ip.
        /// </summary>
        /// <returns></returns>
        public string GetProxyClientIp() 
        {
            try
            {
                return (((IPEndPoint)_tcpProxyClient.Client.RemoteEndPoint).Address).ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Start to handle the proxy client.
        /// </summary>
        public async Task<bool> HandleProxyClient()
        {
            if (await OpenSeedNodeProxyLink())
            {
                ListenRequestClient();
                ListenRequestSeedNode();
                CheckConnectivity();
                return true;
            }
            CloseProxyClient(false);
            return false;
        }

        /// <summary>
        /// Open the seed node proxy link.
        /// </summary>
        private async Task<bool> OpenSeedNodeProxyLink()
        {
            try
            {
                string seedNodeIp = _proxySetting.SeedNodeHostList[ClassUtils.GetRandomBetween(0, _proxySetting.SeedNodeHostList.Count - 1)];

                while (seedNodeIp == _proxySetting.ServerIp
                    || string.IsNullOrEmpty(seedNodeIp))
                {
                    seedNodeIp = _proxySetting.SeedNodeHostList[ClassUtils.GetRandomBetween(0, _proxySetting.SeedNodeHostList.Count - 1)];
                }

                SeedNodeHostTarget = seedNodeIp;

                await _tcpSeedNodeClient.ConnectAsync(SeedNodeHostTarget, _seedNodePort);

                _logSystem.WriteLine("Connect to " + SeedNodeHostTarget + ":" + _seedNodePort + " seed node successfully done for client IP: " + GetProxyClientIp(), LogEnum.SERVER, ConsoleColor.Green, false);

                return true;
            }
            catch(Exception error)
            {
                _logSystem.WriteLine("Failed to connect to the seed node host target: " + SeedNodeHostTarget+" | Exception: "+error.Message, LogEnum.SERVER, ConsoleColor.Red, false);
            }
            return false;
        }

        /// <summary>
        /// Listen the seed node client.
        /// </summary>
        private void ListenRequestSeedNode()
        {
            try
            {
                Task.Factory.StartNew(async () => await DoProxyingPacket(_tcpSeedNodeClient, _tcpProxyClient, false), _proxyClientTokenSource.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Listen proxy client request(s).
        /// </summary>
        private void ListenRequestClient()
        {
            try
            {
                Task.Factory.StartNew(async () => await DoProxyingPacket(_tcpProxyClient, _tcpSeedNodeClient, true), _proxyClientTokenSource.Token, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored catch the exception once the task has been cancelled.
            }
        }
       
        /// <summary>
        /// Check the connectivity of the proxy client and the seed node client link.
        /// </summary>
        private void CheckConnectivity()
        {
            try
            {
                Task.Factory.StartNew(async () =>
                {
                    bool clientIsDead = false;
                    bool seedIsDead = false;

                    while (ProxyClientStatus)
                    {
                        clientIsDead = ClassUtils.TcpClientIsConnected(_tcpProxyClient);

                        seedIsDead = ClassUtils.TcpClientIsConnected(_tcpSeedNodeClient);

                        if (!clientIsDead || 
                            !seedIsDead)
                            break;

                        await Task.Delay(1000);
                    }


                    CloseProxyClient(clientIsDead ? true : false);

                }, _proxyClientTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
            }
            catch
            {
                // Ignored, catch the exception once the task is cancelled.
            }
        }

        /// <summary>
        /// Do the proxying packet listening and sending.
        /// </summary>
        /// <param name="sourceClient"></param>
        /// <param name="destClient"></param>
        /// <returns></returns>
        private async Task DoProxyingPacket(TcpClient sourceClient, TcpClient destClient, bool fromClient)
        {
            try
            {
                byte[] packetData = new byte[8192];

                using (NetworkStream networkStream = new NetworkStream(sourceClient.Client))
                {
                    while (ProxyClientStatus)
                    {
                        int dataLength = 0;
                        while ((dataLength = await networkStream.ReadAsync(packetData, 0, packetData.Length)) > 0)
                        {

                            //packetData = packetData.SkipWhile(x => x != 0).ToArray();

                            _logSystem.WriteLine("Packet data received from "
                                + (fromClient ? "Client: " + GetProxyClientIp() : "Server: " + SeedNodeHostTarget +":"+_seedNodePort) +
                                " is " + Encoding.UTF8.GetString(packetData), fromClient ? _clientEnum : _seedEnum, ConsoleColor.Red, false);

                            if (!await SendPacketToTarget(destClient, packetData))
                                break;
                        }
                    }
                }
            }
            catch(Exception error)
            {
                _logSystem.WriteLine("Failed to listen and proxying packet from " + (fromClient ? GetProxyClientIp() : SeedNodeHostTarget + ":"+_seedNodePort) + " | Exception: " + error.Message, fromClient ? _clientEnum : _seedEnum, ConsoleColor.Red, false);
            }
        }

        /// <summary>
        /// Send packet data to the target.
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="packetData"></param>
        /// <returns></returns>
        private async Task<bool> SendPacketToTarget(TcpClient tcpClient, byte[] packetData)
        {
            try
            {

                byte[] dataCleaned = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(packetData).Replace("\0", ""));
#if DEBUG
                Debug.WriteLine("Packet data to send: " + Encoding.UTF8.GetString(packetData).Replace("\0", ""));
#endif
                using (NetworkStream networkStream = new NetworkStream(tcpClient.Client))
                {
                    await networkStream.WriteAsync(dataCleaned, 0, dataCleaned.Length, _proxyClientTokenSource.Token);
                    await networkStream.FlushAsync(_proxyClientTokenSource.Token);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Close the proxy client.
        /// </summary>
        public void CloseProxyClient(bool fromClient)
        {
            if (ProxyClientStatus)
            {

                _logSystem.WriteLine(GetProxyClientIp() + " connected to seed node host port: " + _seedNodePort + " is dead.", fromClient ? _clientEnum : _seedEnum , ConsoleColor.Red, false);

                ProxyClientStatus = false;


                #region Close the proxy tcp client.

                try
                {
                    _tcpProxyClient?.Close();
                    _tcpProxyClient?.Dispose();
                }
                catch
                {
                    // Ignored.
                }

                #endregion

                #region Close the seed node tcp client.

                try
                {
                    _tcpSeedNodeClient?.Close();
                    _tcpSeedNodeClient?.Dispose();
                }
                catch
                {
                    // Ignored.
                }

                #endregion

                #region Close the client token source.

                try
                {
                    if (!_proxyClientTokenSource.IsCancellationRequested)
                        _proxyClientTokenSource.Cancel();
                }
                catch
                {
                    // Ignored.
                }

                #endregion

            }

        }
    }
}
